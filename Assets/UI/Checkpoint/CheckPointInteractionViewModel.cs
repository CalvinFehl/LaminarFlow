using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CheckPointInteractionViewModel : MonoBehaviour
{
    private TextElement text;
    public UIDocument document;

    [SerializeField]
    [TextArea(3, 10)]
    private String startText;
    public String StartText
    {
        get => startText;
        set
        {
            startText = value;
            text.text = StartText;
        }
    }

    public void Awake()
    {

        text = GetComponent<UIDocument>().rootVisualElement.Q<TextElement>("CheckpointStartText");
        text.text = StartText;

    }
}
