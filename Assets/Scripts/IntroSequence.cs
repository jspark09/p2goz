using UnityEngine;

public class IntroSequence : MonoBehaviour
{
    public GameObject[] screens;
    private int index = 0;

    void Start()
    {
        for (int i = 0; i < screens.Length; i++)
            screens[i].SetActive(i == 0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NextScreen();
        }
    }

    void NextScreen()
    {
        screens[index].SetActive(false);
        index++;

        if (index < screens.Length)
        {
            screens[index].SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
