using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Button : MonoBehaviour {

	public void UpButton()
    {
        gameObject.GetComponent<Slider>().value += 1f;
        
    
    }

    public void DownButton()
    {
        gameObject.GetComponent<Slider>().value -= 1f;
    }
}
