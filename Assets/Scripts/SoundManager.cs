using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour {

    public AudioSource seSource;
    public AudioSource bgmSource;

    public AudioClip generateSE;


    public void PlayGenerateSE()
    {
        seSource.PlayOneShot(generateSE);
    }
}
