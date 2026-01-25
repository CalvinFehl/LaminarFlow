using Assets.Scripts.CheckpointSystem.Configurators;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;



public class HighscoreViewModel : MonoBehaviour
{
    public UIDocument document;
    public VisualTreeAsset ListEntry;

    private ListView listView;

    
    public List<PlayerSprintInfo> HighscoreList
    {
        get => (List<PlayerSprintInfo>)listView.itemsSource;
        set => listView.itemsSource = value;
    }
    // Start is called before the first frame update
    void Awake()
    {
        listView = document.rootVisualElement.Q<ListView>("HighscoreListView");
        listView.makeItem = () =>
        {
            var newListEntry = ListEntry.Instantiate();
            newListEntry.userData = new SprintHighscoreListEntryViewModel();
            return newListEntry;
        };
        listView.bindItem = (item,index) =>
        {
            var sprintHSVM = (SprintHighscoreListEntryViewModel)item.userData;
            sprintHSVM.Bind(item);
            var sprintinfo = HighscoreList[index];
            sprintHSVM.PlayerName = sprintinfo.Player.name;
            sprintHSVM.Time = sprintinfo.ChallengeTime;
            sprintHSVM.Rank = index + 1;

        };

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
