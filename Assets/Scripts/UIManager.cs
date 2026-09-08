using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    [SerializeField] private GameObject displayPanel;
    [SerializeField] private TextMeshProUGUI displayName;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private GameObject warningPrefab;
    private GameObject currentlySelected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedCreature() != null)
        {
            displayPanel.SetActive(true);
        }
        else
        {
            displayPanel.SetActive(false);
        }
    }

    void Update()
    {
        DisplayInfo();
    }

    public void DisplayInfo()
    {
        if (currentlySelected != null)
        {
            if (currentlySelected.GetComponent<Creature>())
            {
                displayPanel.SetActive(true);
                displayName.text = currentlySelected.GetComponent<Creature>().GetName();

                string formattedInfo = "";
                List<Stat> selectedCreatureStats = currentlySelected.GetComponent<Creature>().GetStats();

                foreach (Stat stat in selectedCreatureStats)
                {
                    formattedInfo += stat.ToString();
                    formattedInfo += "\n";
                }
                displayText.text = formattedInfo;
            }
            else if (currentlySelected.GetComponent<Room>())
            {
                displayPanel.SetActive(true);
                displayName.text = currentlySelected.GetComponent<Room>().GetRoomName();
                displayText.text = currentlySelected.GetComponent<Room>().ToString();
            }
            else
            {
                ClearDisplay();
            }
        }
        else
        {
            displayPanel.SetActive(false);
        }
    }

    private void ClearDisplay()
    {
        displayPanel.SetActive(false);
        displayName.text = "";
        displayText.text = "";
        currentlySelected = null;
    }

    /// <summary>
    /// Instantiates an on-screen warning banner alert for layout feedback.
    /// Accessible globally via UIManager.Instance.DisplayWarningMessage("Your Text");
    /// </summary>
    public void DisplayWarningMessage(string warningMessage)
    {
        if (warningPrefab == null || warningPanel == null) return;

        GameObject warningGameObject = Instantiate(warningPrefab, warningPanel.transform);
        TextMeshProUGUI warningText = warningGameObject.GetComponentInChildren<TextMeshProUGUI>();

        if (warningText != null)
        {
            warningText.text = warningMessage;
        }

        Destroy(warningGameObject, 3f);
    }

    public void SetCurrentlySelected(GameObject selected)
    {
        currentlySelected = selected;
    }
}
