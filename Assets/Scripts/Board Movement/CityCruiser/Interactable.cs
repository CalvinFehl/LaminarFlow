using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class InteractionEventData : BaseEventData
{
    public GameObject Interactor { get; set; }
    public InteractionEventData(EventSystem eventSystem, GameObject interactor = null) : base(eventSystem)
    {
        Interactor = interactor;
    }
}

[Serializable]
public class InteractionEvent : UnityEvent<InteractionEventData>
{
}

public class Interactable : MonoBehaviour
{

    public float radius = 3f;
    public bool autoInteraction = false;

    // callback function
    public InteractionEvent interactionCallback;
    public InteractionEvent inReachCallback;
    public InteractionEvent outofReachCallback;


    private bool isinReach = false;
    private GameObject interactor;

    public KeyCode InteractionKey = KeyCode.UpArrow;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (isinReach)
        {
            if (interactor != null && interactor is GameObject && Input.GetKeyDown(InteractionKey))
            {
                Interact(new InteractionEventData(EventSystem.current, interactor));
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isinReach)
        {
            interactor = other.gameObject;
            isinReach = true;

            var eventData = new InteractionEventData(EventSystem.current, interactor);
            inReachCallback?.Invoke(eventData);

            if (autoInteraction)
            {
                Interact(eventData);
            }
        }
        
    }

    private void OnTriggerExit(Collider other)
    {
        if (isinReach)
        {
            isinReach = false;
            var eventData = new InteractionEventData(EventSystem.current, interactor);
            outofReachCallback?.Invoke(eventData);

            interactor = null;
        }
    }

    public void Interact(InteractionEventData eventData)
    {
        if (eventData.Interactor != null && eventData.Interactor is GameObject)
        {
            interactionCallback?.Invoke(eventData);
        }
    }
}

