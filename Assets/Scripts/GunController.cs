using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunController : MonoBehaviour
{
    public bool isAutomatic;
    public float heatPerShot = 1f, timeBeetwenShots = .1f;
    public int shotDamage;

    public GameObject muzzleFlash;
    public AudioSource shotSound;
}
