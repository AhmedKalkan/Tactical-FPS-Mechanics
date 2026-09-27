using UnityEngine;

public class KarakterHareket : MonoBehaviour
{
    public Animator anim;
    public Transform kamera;
    private Rigidbody rb;

    [Header("BLENDER EKSEN DÜZELTÝCÝ (TIKLA VE ÇÖZ)")]
    public bool eksenlerYerDegismis = true;
    public bool yatmaYonuTers = true; // Ýkisini de açýnca çalýþmýþtý, varsayýlan yaptým!

    [Header("FÝZÝKSEL HIZ AYARLARI")]
    public float yurumeHizi = 5f;
    public float kosmaHizi = 8f;
    public float puruzsuzluk = 10f;
    public float fareHassasiyeti = 300f;

    [Header("MERKEZKAÇ ÝVMESÝ")]
    public float toparlanmaIvmesi = 5f;

    [Header("KOÞMA MERKEZKAÇ AYARLARI")]
    public float kosmaYanaYatma = 20f;
    public float kosmaOneEgilme = 15f;

    [Header("YÜRÜME MERKEZKAÇ AYARLARI")]
    public float yurumeYanaYatma = 5f;  // Yürürken daha az yatar
    public float yurumeOneEgilme = 2f;  // Yürürken MJ efekti neredeyse olmaz

    [Header("Geliþmiþ Anatomi (Esnek Omurga)")]
    public Transform armature;
    public Transform omurgaKemigi;
    public Transform kafaKemigi;
    [Range(0f, 1f)] public float omurgaSabitlikOrani = 0.812f;

    [Header("YÜRÜME VE KOÞMA ZIPLAMA")]
    public float yurumeZiplamaYuksekligi = 0.1f;
    public float yurumeYumusatma = 25f;
    [Range(0f, 1f)] public float yurumeZamanlamasi = 0f;
    public float kosmaZiplamaYuksekligi = 0.15f;
    public float kosmaYumusatma = 35f;
    [Range(0f, 1f)] public float kosmaZamanlamasi = 0f;

    private float mevcutX = 0f, mevcutY = 0f, mevcutAnimX = 0f, mevcutAnimY = 0f, xRotasyon = 0f;
    private float mevcutHiz = 5f;
    private Vector3 normalKameraPos, onKameraPos;
    private float normalKameraRotY, onKameraRotY, anlikKameraRotY;
    private bool ondenGorunum = false, oncekiKaredeHareket = false;

    // Kemik Baþlangýç Hafýzasý
    private float baslangicArmatureX;
    private float baslangicArmatureY;
    private float baslangicArmatureZ;
    private Vector3 baslangicArmaturePos;

    private float mevcutBedenAcisi = 0f;
    private float aktifYukseklik = 0.1f, aktifYumusatma = 25f, aktifZamanlama = 0f, mevcutSekmeY = 0f;

    // Dönüþ Sistemi Deðiþkenleri
    private float anlikDonusHizi = 0f;
    private float mevcutDonusEtkisi = 0f;
    private float anlikYatmaAcisi = 0f;
    private float anlikOneEgilme = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();

        normalKameraPos = kamera.localPosition;
        normalKameraRotY = kamera.localEulerAngles.y;
        anlikKameraRotY = normalKameraRotY;
        onKameraPos = new Vector3(-normalKameraPos.x, normalKameraPos.y, Mathf.Abs(normalKameraPos.z));
        onKameraRotY = normalKameraRotY + 180f;

        if (armature != null)
        {
            baslangicArmatureX = armature.localEulerAngles.x;
            baslangicArmatureY = armature.localEulerAngles.y;
            baslangicArmatureZ = armature.localEulerAngles.z;
            baslangicArmaturePos = armature.localPosition;
        }

        mevcutHiz = yurumeHizi;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V)) ondenGorunum = !ondenGorunum;
        Vector3 hedefKameraPos = ondenGorunum ? onKameraPos : normalKameraPos;
        kamera.localPosition = Vector3.Lerp(kamera.localPosition, hedefKameraPos, Time.deltaTime * 8f);
        anlikKameraRotY = Mathf.LerpAngle(anlikKameraRotY, (ondenGorunum ? onKameraRotY : normalKameraRotY), Time.deltaTime * 8f);

        float gercekMouseX = Input.GetAxis("Mouse X") * fareHassasiyeti;
        transform.Rotate(Vector3.up * (gercekMouseX * Time.deltaTime));

        float mouseY = Input.GetAxis("Mouse Y") * fareHassasiyeti * Time.deltaTime;
        xRotasyon = Mathf.Clamp(xRotasyon - mouseY, -60f, 60f);
        kamera.localRotation = Quaternion.Euler(xRotasyon, anlikKameraRotY, 0f);

        // --- MERKEZKAÇ HESAPLAMASI ---
        anlikDonusHizi = Mathf.Lerp(anlikDonusHizi, gercekMouseX, Time.deltaTime * 10f);

        float fizikX = Input.GetAxisRaw("Horizontal");
        float fizikY = Input.GetAxisRaw("Vertical");

        bool shiftBasili = Input.GetKey(KeyCode.LeftShift);
        bool kosuyor = shiftBasili && (fizikY > 0);
        bool suAnHareketVar = (fizikX != 0 || fizikY != 0);

        float hedefDonusEtkisi = 0f;
        float hedefYatma = 0f;
        float hedefOneEgilme = 0f;

        if (suAnHareketVar)
        {
            float donusSiddeti = Mathf.Clamp01(Mathf.Abs(anlikDonusHizi) / 350f);

            if (kosuyor) hedefDonusEtkisi = donusSiddeti;

            // KOÞMA VE YÜRÜME ÝÇÝN AYRI AÇILARI SEÇÝYORUZ
            float aktifYanaYatma = kosuyor ? kosmaYanaYatma : yurumeYanaYatma;
            float aktifOneEgilme = kosuyor ? kosmaOneEgilme : yurumeOneEgilme;

            float yatmaSiddeti = Mathf.Clamp(anlikDonusHizi / 350f, -1f, 1f);
            hedefYatma = yatmaSiddeti * aktifYanaYatma;

            if (fizikY > 0)
            {
                hedefOneEgilme = donusSiddeti * aktifOneEgilme;
            }
            else if (fizikY < 0)
            {
                hedefYatma = -hedefYatma;
                hedefOneEgilme = 0f;
            }
        }

        // Deðerleri Pürüzsüzleþtirme
        mevcutDonusEtkisi = Mathf.Lerp(mevcutDonusEtkisi, hedefDonusEtkisi, Time.deltaTime * toparlanmaIvmesi);
        anlikYatmaAcisi = Mathf.Lerp(anlikYatmaAcisi, hedefYatma, Time.deltaTime * toparlanmaIvmesi);
        anlikOneEgilme = Mathf.Lerp(anlikOneEgilme, hedefOneEgilme, Time.deltaTime * toparlanmaIvmesi);

        // Hýz Düþürme Sistemi
        float hedefHiz = kosuyor ? Mathf.Lerp(kosmaHizi, yurumeHizi, mevcutDonusEtkisi) : yurumeHizi;
        float ileriAnimDegeri = kosuyor ? Mathf.Lerp(2f, 1f, mevcutDonusEtkisi) : 1f;

        // --- STANDART ÇAPRAZ GEÇÝÞLER ---
        float animX = fizikX;
        float animY = fizikY;
        float hedefBedenAcisi = 0f;

        if (fizikY > 0) animY = ileriAnimDegeri;
        if (fizikX > 0 && fizikY > 0) { animX = 0f; animY = ileriAnimDegeri; hedefBedenAcisi = 45f; }
        else if (fizikX < 0 && fizikY > 0) { animX = 0f; animY = ileriAnimDegeri; hedefBedenAcisi = -45f; }
        else if (fizikX > 0 && fizikY < 0) { animX = 0f; animY = -1f; hedefBedenAcisi = -45f; }
        else if (fizikX < 0 && fizikY < 0) { animX = 0f; animY = -1f; hedefBedenAcisi = 45f; }

        if (suAnHareketVar && !oncekiKaredeHareket) anim.Play("Hareket", 0, 0f);
        oncekiKaredeHareket = suAnHareketVar;

        mevcutHiz = Mathf.Lerp(mevcutHiz, hedefHiz, Time.deltaTime * puruzsuzluk);
        mevcutX = Mathf.Lerp(mevcutX, fizikX, Time.deltaTime * puruzsuzluk);
        mevcutY = Mathf.Lerp(mevcutY, fizikY, Time.deltaTime * puruzsuzluk);

        if (Mathf.Abs(mevcutX) < 0.001f) mevcutX = 0f;
        if (Mathf.Abs(mevcutY) < 0.001f) mevcutY = 0f;

        mevcutAnimX = Mathf.Lerp(mevcutAnimX, animX, Time.deltaTime * puruzsuzluk);
        mevcutAnimY = Mathf.Lerp(mevcutAnimY, animY, Time.deltaTime * puruzsuzluk);
        mevcutBedenAcisi = Mathf.Lerp(mevcutBedenAcisi, hedefBedenAcisi, Time.deltaTime * (puruzsuzluk / 1.5f));

        float dinamikZiplamaYuksekligi = kosuyor ? Mathf.Lerp(kosmaZiplamaYuksekligi, yurumeZiplamaYuksekligi, mevcutDonusEtkisi) : yurumeZiplamaYuksekligi;
        float dinamikYumusatma = kosuyor ? Mathf.Lerp(kosmaYumusatma, yurumeYumusatma, mevcutDonusEtkisi) : yurumeYumusatma;
        float dinamikZamanlama = kosuyor ? Mathf.Lerp(kosmaZamanlamasi, yurumeZamanlamasi, mevcutDonusEtkisi) : yurumeZamanlamasi;

        aktifYukseklik = Mathf.Lerp(aktifYukseklik, dinamikZiplamaYuksekligi, Time.deltaTime * puruzsuzluk);
        aktifYumusatma = Mathf.Lerp(aktifYumusatma, dinamikYumusatma, Time.deltaTime * puruzsuzluk);
        aktifZamanlama = Mathf.Lerp(aktifZamanlama, dinamikZamanlama, Time.deltaTime * puruzsuzluk);

        float hedefSekmeY = 0f;
        if (suAnHareketVar)
        {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            float animZaman = stateInfo.normalizedTime;
            float ayarliZaman = animZaman + aktifZamanlama;
            float adimEvresi = (ayarliZaman % 0.5f) * 2f;
            hedefSekmeY = Mathf.Sin(adimEvresi * Mathf.PI) * aktifYukseklik;
        }

        mevcutSekmeY = Mathf.Lerp(mevcutSekmeY, hedefSekmeY, Time.deltaTime * aktifYumusatma);

        if (anim != null) { anim.SetFloat("DirX", mevcutAnimX); anim.SetFloat("DirY", mevcutAnimY); }
    }

    void FixedUpdate()
    {
        Vector3 gercekIleri = Quaternion.Euler(0, transform.eulerAngles.y + normalKameraRotY, 0) * Vector3.forward;
        Vector3 gercekSag = Quaternion.Euler(0, transform.eulerAngles.y + normalKameraRotY, 0) * Vector3.right;
        Vector3 hareketYonu = (gercekIleri * mevcutY) + (gercekSag * mevcutX);

        if (hareketYonu.magnitude > 1f) hareketYonu.Normalize();

        if (rb != null)
        {
            if (hareketYonu.magnitude == 0f) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            else
            {
                Vector3 fizikselHiz = hareketYonu * mevcutHiz;
                fizikselHiz.y = rb.linearVelocity.y;
                rb.linearVelocity = fizikselHiz;
            }
        }
    }

    void LateUpdate()
    {
        if (armature != null)
        {

            armature.localEulerAngles = new Vector3(baslangicArmatureX, baslangicArmatureY + mevcutBedenAcisi, baslangicArmatureZ);

            float finalYatma = yatmaYonuTers ? anlikYatmaAcisi : -anlikYatmaAcisi;

            if (eksenlerYerDegismis)
            {
                armature.Rotate(finalYatma, 0f, anlikOneEgilme, Space.Self);
            }
            else
            {
                armature.Rotate(anlikOneEgilme, 0f, finalYatma, Space.Self);
            }

            Vector3 armPos = armature.localPosition;
            armPos.y = baslangicArmaturePos.y + mevcutSekmeY;
            armature.localPosition = armPos;
        }

        if (omurgaKemigi != null)
        {
            Vector3 omurgaEulers = omurgaKemigi.localEulerAngles;
            omurgaEulers.y -= (mevcutBedenAcisi * omurgaSabitlikOrani);
            omurgaKemigi.localEulerAngles = omurgaEulers;
        }
        if (kafaKemigi != null)
        {
            Vector3 kafaEulers = kafaKemigi.localEulerAngles;
            kafaEulers.y -= (mevcutBedenAcisi * (1f - omurgaSabitlikOrani));
            kafaKemigi.localEulerAngles = kafaEulers;
        }
    }
}