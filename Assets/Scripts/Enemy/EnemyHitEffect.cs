using UnityEngine;
using System.Collections;



public class EnemyHitEffect : MonoBehaviour
{

    private SpriteRenderer sr;


    private Color originalColor;



    void Start()
    {

        sr = GetComponent<SpriteRenderer>();

        originalColor = sr.color;

    }



    public void HitFlash()
    {

        StartCoroutine(Flash());

    }



    IEnumerator Flash()
    {

        sr.color = Color.white;


        yield return new WaitForSeconds(0.1f);


        sr.color = originalColor;


    }

}