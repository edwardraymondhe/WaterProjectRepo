using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI : MonoBehaviour
{
    public void SwitchActive()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }
}
