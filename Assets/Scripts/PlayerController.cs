using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
public class PlayerController : MonoBehaviourPunCallbacks
{

    public Transform viewPoint;

    //mouse hassasiyeti de�i�keni
    public float mouseSensitivity =1f;

    //bak�� a��s�n� ne kadar d�nd�rmek istedi�imizi sa�layacak de�i�ken (yukar� ve a�a��)
    private float verticalRotStore;

    private Vector2 mouseInput;

    //terse bakma de�i�keni t/f
    public bool invertLook;

    // player h�z de�i�keni
    public float moveSpeed=5f;

    //ko�u h�z�
    public float runSpeed = 8f;

    private float activeMoveSpeed;

    //hareket y�n�
    private Vector3 moveDirection;

    //hareket
    private Vector3 movement;

    public CharacterController charCon;

    private Camera cam;

    public Camera minimapCam;

    private MiniMap minimap;

    //Z�plama kuvveti
    public float jumpForce = 12f;
    //yer�ekimi kuvveti
    public float gravityMod = 2.5f;

    public Transform groundCheckPoint;
    public bool isGrounded;
    public LayerMask groundLayers;

    public GameObject bulletImpact;
    //ate� etme aras� s�re
    //public float timebetweenshots = .1f;
    //ate� etme sayac�
    private float shotCounter;
    //muzzle g�z�kme zaman�
    public float muzzleDisplayTime;
    //muzzle sayac�
    private float muzzleCounter;

    //silah �s�nmas� de�i�kenleri

    public float maxHeat = 10f, /*heatPerShot = 1f,*/ coolSpeed = 4f, overHeatCoolSpeed = 5f;
    public float heatCounter;
    public bool overHeated;

    public GunController[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    public Animator anim;

    public GameObject playerModel;

    public Transform modelGunPoint, gunHolder;

    public Material[] allSkins;

    public AudioSource footstepFast, footstepSlow;

    // Start is called before the first frame update
    void Start()
    {
        //imlecimizin ekran�n ortas�na kilitlenmesi i�in
        Cursor.lockState = CursorLockMode.Locked;

        cam = Camera.main;

        UIController.instance.heatSlider.maxValue = maxHeat;

        //SwitchGun();

        photonView.RPC("SetGun", RpcTarget.All, selectedGun);

        currentHealth = maxHealth;

        minimap = GameObject.FindObjectOfType<MiniMap>();

        if (photonView.IsMine)
        {
            minimap.AddPlayer(photonView.ViewID, transform);
            playerModel.SetActive(false);

            UIController.instance.healthSlider.maxValue = maxHealth;
            UIController.instance.healthSlider.value = currentHealth;
        }
        else
        {
            gunHolder.parent = modelGunPoint;
            gunHolder.localPosition = Vector3.zero;
            gunHolder.localRotation = Quaternion.identity;
        }

        Material material = allSkins[photonView.Owner.ActorNumber % allSkins.Length];
        playerModel.GetComponent<Renderer>().material = material;
       /* Transform newTransform = SpawnManager.instance.GetSpawnPoint();
        transform.position = newTransform.position;
        transform.rotation = newTransform.rotation;*/
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView.IsMine)
        {


            //Mouse hareketinin sa�lanmas�
            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y") * mouseSensitivity);

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotStore += mouseInput.y;
            verticalRotStore = Mathf.Clamp(verticalRotStore, -60f, 60f);
            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            //player hareketinin sa�lanmas�
            moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;
                if (!footstepFast.isPlaying && moveDirection != Vector3.zero )
                {
                    footstepFast.Play();
                    footstepSlow.Stop();
                }

            }
            else
            {
                activeMoveSpeed = moveSpeed;

                if (!footstepSlow.isPlaying && moveDirection != Vector3.zero)
                {
                    footstepFast.Stop();
                    footstepSlow.Play();
                }
            }

            if (moveDirection == Vector3.zero || !isGrounded)
            {
                footstepSlow.Stop();
                footstepFast.Stop();
            }
            //yer�ekimi i�in kullanaca��m y h�z�
            float yVelocity = movement.y;

            movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;

            movement.y = yVelocity;
            if (charCon.isGrounded)
            {
                movement.y = 0f;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

            if (Input.GetKeyDown(KeyCode.Space) && charCon.isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityMod;

            charCon.Move(movement * Time.deltaTime);

            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {

                muzzleCounter -= Time.deltaTime;
                if (muzzleCounter <= 0)
                {
                    //muzzle falsh kapal�
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }

            }

            //muzzle falsh kapal�
            allGuns[selectedGun].muzzleFlash.SetActive(false);

            if (!overHeated)
            {

                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)

                {
                    shotCounter -= Time.deltaTime;
                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }
                heatCounter -= coolSpeed * Time.deltaTime;
            }
            else
            {
                heatCounter -= overHeatCoolSpeed * Time.deltaTime;
                if (heatCounter <= 0)
                {

                    overHeated = false;

                    UIController.instance.asiriIsinmisMessage.gameObject.SetActive(false);

                }
            }
            if (heatCounter < 0)
            {
                heatCounter = 0f;
            }

            UIController.instance.heatSlider.value = heatCounter;

            // mouse tekerle�iyle silah de�i�me

            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;

                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }

                // SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);

            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;

                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;

                }
                //SwitchGun();
                photonView.RPC("SetGun", RpcTarget.All, selectedGun);

            }

            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    //SwitchGun();
                    photonView.RPC("SetGun", RpcTarget.All, selectedGun);

                }
            }
            


            anim.SetBool("grounded", isGrounded);
            //magnitude ne kadar mesafe katedildi�ini tek bir say�yla s�yler. pozitif
            anim.SetFloat("speed", moveDirection.magnitude);




            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0) && !UIController.instance.optionsScreen.activeInHierarchy)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
      

    }
    private void OnDestroy()
    {
        // Minimap'den oyuncuyu kald�r
        if (photonView.IsMine)
        {
            minimap.RemovePlayer(photonView.ViewID);
        }
    }
    private void Shoot()
    {
        //ekran�n tam ortas�
        Ray ray = cam.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = cam.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log("Vuruldu" + hit.collider.gameObject.name);

            if (hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Hit" + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shotDamage,PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * .003f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 5f);
            }
        }
        shotCounter = allGuns[selectedGun].timeBeetwenShots;


        heatCounter += allGuns[selectedGun].heatPerShot;

        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;
            UIController.instance.asiriIsinmisMessage.gameObject.SetActive(true);

        }
        //muzle flash aktif
        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;

        allGuns[selectedGun].shotSound.Stop();
        allGuns[selectedGun].shotSound.Play();
    }

    [PunRPC]
    private void DealDamage(string damager,int damageAmount, int actor)
    {
        TakeDamage(damager,damageAmount,actor);
    }

    public void TakeDamage(string damager,int damageAmount,int actor)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damageAmount;
            
            if (currentHealth <= 0)
            {
                currentHealth = 0;

                PlayerSpawner.instance.Die(damager);

                MatchManager.instance.UpdateStatsSend(actor,0,1);

            }
        }
        UIController.instance.healthSlider.value = currentHealth;
        //Debug.Log(photonView.Owner.NickName+damager + " taraf�ndan vuruldu");

    }
  
    // her �eyin ard�ndan ger�ekle�mesini sa�lamak i�in LateUpdate de �zel �eyler yap�labilir
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (MatchManager.instance.state == MatchManager.GameState.Playing)
            {
                cam.transform.position = viewPoint.position;
                cam.transform.rotation = viewPoint.rotation;
            }
            else
            {
                cam.transform.position = MatchManager.instance.mapCamPoint.position;
                cam.transform.rotation = MatchManager.instance.mapCamPoint.rotation;
            }
            
        }

    }
    //silah de�itirmek
    private void SwitchGun()
    {
        foreach (GunController gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }
        allGuns[selectedGun].gameObject.SetActive(true);

        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitchTo)
    {
        if (gunToSwitchTo < allGuns.Length)
        {
            selectedGun = gunToSwitchTo;
            SwitchGun();
        }
    }

}
