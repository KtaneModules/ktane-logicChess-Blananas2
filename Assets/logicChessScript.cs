using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class logicChessScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;

	/*
	SL's standard position: x=0.075167, y=0.01986, z=0.076057
	SL's position in this mod: x=0, y=-0.03125, z=0
	
	[◀◀] [▶]
	*/

    public KMSelectable[] Buttons; //Reds 1 and 2, Options 1, 2, 3
    public GameObject AllOptButtons;
    public GameObject[] IndivOptButtons;
    public TextMesh MainText;
    public TextMesh PersonText;
    public TextMesh[] ButtonTexts; //same as above
    public Material[] ButtonColors; //w,b,r
    public Color[] TextColors; //w,b #6a9ac4, k
    public GameObject PrettyMuchEverything;
    public GameObject[] ModuleShapes;
    public GameObject StatusLight;
    public GameObject[] SpriteObjects; //Person12, piece123
    public SpriteRenderer[] Sprites; //see above
    public Sprite[] PeopleSprites;
    public Sprite[] PieceSprites; //pbnrbk
    public Sprite Nothing;

    private int caseIx = -1;
    private string[] caseData = { };
    private int casePointer = 0;
    private string[] currentData = { };
    private string opponentsName = "";
    private int currentEvent = 0;
    private int[] buttonGotos = { -1, -1, -1 };
    private int numberOfButtons = 0;
    private bool showingButtons = false;
    private int justOneGoto = -1;
    private int restart = 0;
    private bool cantGoBack = false;
    private bool scrolling = false;
    private int chosenPerson = 0;
    private bool edgeworkSpeaking = false;

    //Logging
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake () {
        moduleId = moduleIdCounter++;
        
        foreach (KMSelectable button in Buttons) {
            button.OnInteract += delegate () { buttonPress(button); return false; };
        }
    }

    // Use this for initialization
    void Start () {
        AllOptButtons.SetActive(false);
        StatusLight.transform.localPosition = 0.03125f * Vector3.down;
        GetComponent<KMSelectable>().UpdateChildren();
        caseIx = UnityEngine.Random.Range(0,9);
        ChooseCase(caseIx);
        opponentsName = caseData[0].Split('|')[0];
        chosenPerson = int.Parse(caseData[0].Split('|')[1]);
        caseData = caseData.Skip(1).ToArray();

        currentData = caseData[0].Split('|');
        ShowShit();
    }

    void ChooseCase (int c) {
        switch (c) {
            case 0: caseData = logicChessData.Case0; break;
            case 1: caseData = logicChessData.Case1; break;
            case 2: caseData = logicChessData.Case2; break;
            case 3: caseData = logicChessData.Case3; break;
            case 4: caseData = logicChessData.Case4; break;
            case 5: caseData = logicChessData.Case5; break;
            case 6: caseData = logicChessData.Case6; break;
            case 7: caseData = logicChessData.Case7; break;
            case 8: caseData = logicChessData.Case8; break;
        }
        Debug.LogFormat("<Logic Chess #{0}> caseIx: {1}", moduleId, c);
    }

    void buttonPress(KMSelectable button) {
        for (int b = 0; b < 5; b++) {
            if (Buttons[b] == button) {
                button.AddInteractionPunch();
                GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
                WhatTheFuckDoIDo(b);
            }
        }
    }

    void ShowShit () { //ASSUMES CURRENTDATA IS CORRECT!!!
        restart = 0;
        AllOptButtons.SetActive(false);
        GetComponent<KMSelectable>().UpdateChildren();
        ButtonTexts[1].text = "▶";
        //Array.Clear(currentData, 0, currentData.Count());

        //First character indicates who is speaking.
        if (currentData[0][0] == '+' || currentData[0][0] == '*') { //It was originally just + but + is inconvenient in spreadsheets
            PersonText.text = "Edgework";
            PersonText.transform.localScale = new Vector3(0.1f, 0.15f, 1f);
            Sprites[0].sprite = PeopleSprites[0];
            Sprites[1].sprite = Nothing;
            edgeworkSpeaking = true;
        } else {
            PersonText.text = opponentsName;
            Sprites[0].sprite = Nothing;
            Sprites[1].sprite = PeopleSprites[chosenPerson];
            if (opponentsName.Length <= 5) {
                PersonText.transform.localScale = new Vector3(0.15f, 0.15f, 1f);
            } else {
                switch (opponentsName.Length) {
                    case 6: PersonText.transform.localScale = new Vector3(0.13f, 0.15f, 1f); break;
                    case 7: PersonText.transform.localScale = new Vector3(0.11f, 0.15f, 1f); break;
                    case 8: PersonText.transform.localScale = new Vector3(0.1f, 0.15f, 1f); break;
                    case 9: PersonText.transform.localScale = new Vector3(0.09f, 0.15f, 1f); break;
                    case 10: PersonText.transform.localScale = new Vector3(0.08f, 0.15f, 1f); break;
                    case 11: PersonText.transform.localScale = new Vector3(0.07f, 0.15f, 1f); break;
                    default: PersonText.transform.localScale = new Vector3(0.06f, 0.15f, 1f); break;
                }
            }
            edgeworkSpeaking = false;
        }

        //Second character indicates the text's color.
        if (currentData[0][1] == 'w') {
            MainText.color = TextColors[0];
        } else {
            MainText.color = TextColors[1];
        }

        //Third character indicates the event.
        switch (currentData[0][2]) {
            case '.': currentEvent = 0; cantGoBack = false; break; //nothing interesting happening
            case ',': currentEvent = 0; cantGoBack = true; break;
            case 'X': currentEvent = 2; justOneGoto = int.Parse(currentData[0][3]+currentData[0][4].ToString()); cantGoBack = true; break;
            case '!': currentEvent = 3; cantGoBack = false; break;
            
            case '_': currentEvent = 1; cantGoBack = false;
            switch (currentData[0].Length){
                case 5: //one button
                    numberOfButtons = 1;
                    buttonGotos[0] = int.Parse(currentData[0][3]+currentData[0][4].ToString());
                    switch (currentData[2].ToString()) {
                        case "w": Buttons[2].GetComponent<Renderer>().material = ButtonColors[0]; ButtonTexts[2].color = TextColors[2]; break;
                        case "b": Buttons[2].GetComponent<Renderer>().material = ButtonColors[1]; ButtonTexts[2].color = TextColors[0]; break;
                        case "r": Buttons[2].GetComponent<Renderer>().material = ButtonColors[2]; ButtonTexts[2].color = TextColors[0]; break;
                    }
                    switch (currentData[3].ToString()) {
                        case "p": Sprites[2].sprite = PieceSprites[0]; break;
                        case "b": Sprites[2].sprite = PieceSprites[1]; break;
                        case "n": Sprites[2].sprite = PieceSprites[2]; break;
                        case "r": Sprites[2].sprite = PieceSprites[3]; break;
                        case "q": Sprites[2].sprite = PieceSprites[4]; break;
                        case "k": Sprites[2].sprite = PieceSprites[5]; break;
                    }
                    if (currentData[4].Length <= 25) {
                        ButtonTexts[2].transform.localScale = new Vector3(0.043f, 0.1f, 1f);
                    } else if (currentData[4].Length >= 34) {
                        ButtonTexts[2].transform.localScale = new Vector3(0.023f, 0.15f, 1f);
                    } else {
                        ButtonTexts[2].transform.localScale = new Vector3(0.033f, 0.15f, 1f);
                    }
                    ButtonTexts[2].text = currentData[4];
                break;
                case 7: //two buttons
                    numberOfButtons = 2;
                    buttonGotos[0] = int.Parse(currentData[0][3]+currentData[0][4].ToString());
                    buttonGotos[1] = int.Parse(currentData[0][5]+currentData[0][6].ToString());
                    for (int two = 0; two < 2; two++) {
                        switch (currentData[2][two].ToString()) {
                            case "w": Buttons[2+two].GetComponent<Renderer>().material = ButtonColors[0]; ButtonTexts[2+two].color = TextColors[2]; break;
                            case "b": Buttons[2+two].GetComponent<Renderer>().material = ButtonColors[1]; ButtonTexts[2+two].color = TextColors[0]; break;
                            case "r": Buttons[2+two].GetComponent<Renderer>().material = ButtonColors[2]; ButtonTexts[2+two].color = TextColors[0]; break;
                        }
                        switch (currentData[3][two].ToString()) {
                            case "p": Sprites[2+two].sprite = PieceSprites[0]; break;
                            case "b": Sprites[2+two].sprite = PieceSprites[1]; break;
                            case "n": Sprites[2+two].sprite = PieceSprites[2]; break;
                            case "r": Sprites[2+two].sprite = PieceSprites[3]; break;
                            case "q": Sprites[2+two].sprite = PieceSprites[4]; break;
                            case "k": Sprites[2+two].sprite = PieceSprites[5]; break;
                        }
                        if (currentData[4+two].Length <= 25) {
                            ButtonTexts[2+two].transform.localScale = new Vector3(0.043f, 0.1f, 1f);
                        } else if (currentData[4+two].Length >= 34) {
                            ButtonTexts[2+two].transform.localScale = new Vector3(0.023f, 0.15f, 1f);
                        } else {
                            ButtonTexts[2+two].transform.localScale = new Vector3(0.033f, 0.15f, 1f);
                        }
                    }
                    ButtonTexts[2].text = currentData[4];
                    ButtonTexts[3].text = currentData[5];
                break;
                case 9: //three buttons
                    numberOfButtons = 3;
                    buttonGotos[0] = int.Parse(currentData[0][3]+currentData[0][4].ToString());
                    buttonGotos[1] = int.Parse(currentData[0][5]+currentData[0][6].ToString());
                    buttonGotos[2] = int.Parse(currentData[0][7]+currentData[0][8].ToString());
                    for (int three = 0; three < 3; three++) {
                        switch (currentData[2][three].ToString()) {
                            case "w": Buttons[2+three].GetComponent<Renderer>().material = ButtonColors[0]; ButtonTexts[2+three].color = TextColors[2]; break;
                            case "b": Buttons[2+three].GetComponent<Renderer>().material = ButtonColors[1]; ButtonTexts[2+three].color = TextColors[0]; break;
                            case "r": Buttons[2+three].GetComponent<Renderer>().material = ButtonColors[2]; ButtonTexts[2+three].color = TextColors[0]; break;
                        }
                        switch (currentData[3][three].ToString()) {
                            case "p": Sprites[2+three].sprite = PieceSprites[0]; break;
                            case "b": Sprites[2+three].sprite = PieceSprites[1]; break;
                            case "n": Sprites[2+three].sprite = PieceSprites[2]; break;
                            case "r": Sprites[2+three].sprite = PieceSprites[3]; break;
                            case "q": Sprites[2+three].sprite = PieceSprites[4]; break;
                            case "k": Sprites[2+three].sprite = PieceSprites[5]; break;
                        }
                        if (currentData[4+three].Length <= 25) {
                            ButtonTexts[2+three].transform.localScale = new Vector3(0.043f, 0.1f, 1f);
                        } else if (currentData[4+three].Length >= 34) {
                            ButtonTexts[2+three].transform.localScale = new Vector3(0.023f, 0.15f, 1f);
                        } else {
                            ButtonTexts[2+three].transform.localScale = new Vector3(0.033f, 0.15f, 1f);
                        }
                    }
                    ButtonTexts[2].text = currentData[4];
                    ButtonTexts[3].text = currentData[5];
                    ButtonTexts[4].text = currentData[6];
                break;
            }
            break;
            default: Debug.Log("You are a dumbass."); break;
        }
        
        StartCoroutine(TextScroll(currentData[1]));
        Debug.LogFormat("[Logic Chess #{0}] {1}: \"{2}\"", moduleId, edgeworkSpeaking ? "Edgework" : opponentsName, currentData[1]);
    }

    void WhatTheFuckDoIDo (int p) {
        if (p == 1 && showingButtons) {
            return;
        }

        if (scrolling) {
            StopAllCoroutines();
            MainText.text = WordWrap(currentData[1],26);
            scrolling = false;
            ButtonTexts[1].text = "▶";
            return;
        }

        if (p == 0 && !cantGoBack) {
            if (restart == 0) {
                ButtonTexts[0].text = "?"; restart = 1;
            } else {
                Debug.LogFormat("[Logic Chess #{0}] Module reset requested, going back to start...", moduleId);
                ButtonTexts[0].text = " "; restart = 0;
                casePointer = 0;
                currentData = caseData[casePointer].Split('|');
                showingButtons = false;
                ShowShit();
            }
            return;
        }

        switch (currentEvent) {
            case 0: 
                casePointer += 1;
                currentData = caseData[casePointer].Split('|');
                ShowShit();
            break;
            case 1:
                if (showingButtons) {
                    switch (p) {
                        case 2: 
                        Debug.LogFormat("[Logic Chess #{0}] You chose option 1 ({1})", moduleId, currentData[4]);
                        casePointer = buttonGotos[0]; 
                        currentData = caseData[casePointer].Split('|');
                        ShowShit();
                        break;
                        case 3: 
                        Debug.LogFormat("[Logic Chess #{0}] You chose option 2 ({1})", moduleId, currentData[5]);
                        casePointer = buttonGotos[1]; 
                        currentData = caseData[casePointer].Split('|');
                        ShowShit();
                        break;
                        case 4: 
                        Debug.LogFormat("[Logic Chess #{0}] You chose option 3 ({1})", moduleId, currentData[6]);
                        casePointer = buttonGotos[2]; 
                        currentData = caseData[casePointer].Split('|');
                        ShowShit();
                        break;
                        default:
                        break;
                    }
                    showingButtons = false;
                } else {
                    AllOptButtons.SetActive(true);
                    switch (numberOfButtons) {
                        case 1: IndivOptButtons[0].SetActive(true); IndivOptButtons[1].SetActive(false); IndivOptButtons[2].SetActive(false); Debug.LogFormat("[Logic Chess #{0}] ({1})", moduleId, currentData[4]); break;
                        case 2: IndivOptButtons[0].SetActive(true); IndivOptButtons[1].SetActive(true); IndivOptButtons[2].SetActive(false); Debug.LogFormat("[Logic Chess #{0}] ({1}) ({2})", moduleId, currentData[4], currentData[5]); break;
                        case 3: IndivOptButtons[0].SetActive(true); IndivOptButtons[1].SetActive(true); IndivOptButtons[2].SetActive(true); Debug.LogFormat("[Logic Chess #{0}] ({1}) ({2}) ({3})", moduleId, currentData[4], currentData[5], currentData[6]); break;
                    }
                    GetComponent<KMSelectable>().UpdateChildren();
                    ButtonTexts[1].text = " ";
                    MainText.text = " ";
                    PersonText.text = " ";
                    showingButtons = true;
                }
            break;
            case 2:
                GetComponent<KMBombModule>().HandleStrike();
                Debug.LogFormat("[Logic Chess #{0}] STRIKE!", moduleId);
                casePointer = justOneGoto;
                currentData = caseData[casePointer].Split('|');
                ShowShit();
                break;
            case 3:
                GetComponent<KMBombModule>().HandlePass();
                if (caseIx == 7)
                    StartCoroutine(KanyeFade());
                else
                {
                    PrettyMuchEverything.SetActive(false);
                    ModuleShapes[0].SetActive(true);
                    ModuleShapes[1].SetActive(false);
                    StatusLight.transform.localPosition = new Vector3(0.075167f, 0.01986f, 0.076057f);
                }
                Debug.LogFormat("[Logic Chess #{0}] Module solved.", moduleId);
            break;
            default: Debug.Log("Dipshit alert!!!"); break;
        }

        if (casePointer != 0) {
            ButtonTexts[0].text = "◀◀";
        }
    }

    IEnumerator TextScroll(string s) {
        scrolling = true;
        ButtonTexts[1].text = " ";
        MainText.text = "";
        string x = WordWrap(s, 26);
        for (int i = 0; i < x.Length; i++) {
            MainText.text += x[i];
            if (MainText.text[i] != ' ') {
                yield return new WaitForSeconds(.05f);
            }
        }
        scrolling = false;
        ButtonTexts[1].text = "▶";
    }
    IEnumerator KanyeFade()
    {
        Sprites[1].gameObject.SetActive(true);
        Sprites[1].sprite = Sprites[1].sprite = PeopleSprites[7];
        MainText.text = string.Empty;
        float delta = 0f;
        while (delta < 1)
        {
            delta += 0.33f * Time.deltaTime;
            Sprites[1].color = Color.Lerp(Color.white, Color.clear, delta);
            yield return null;
        }
        StartCoroutine(TextScroll("(What just happened?)"));
        yield return new WaitForSeconds(2);
        PrettyMuchEverything.SetActive(false);
        ModuleShapes[0].SetActive(true);
        ModuleShapes[1].SetActive(false);
        StatusLight.transform.localPosition = new Vector3(0.075167f, 0.01986f, 0.076057f);
    }
    
    /// WORDWRAP CODE, Credit goes to ICR and Rapptz on StackExchange

    static char[] splitChars = new char[] { ' ', '-', '\t' };

    private static string WordWrap(string str, int width)
    {
        string[] words = Exp(str, splitChars);

        int curLineLength = 0;
        StringBuilder strBuilder = new StringBuilder();
        for(int i = 0; i < words.Length; i += 1)
        {
            string word = words[i];
            if (curLineLength + word.Length > width)
            {
                if (curLineLength > 0)
                {
                    strBuilder.Append(Environment.NewLine);
                    curLineLength = 0;
                }

                while (word.Length > width)
                {
                    strBuilder.Append(word.Substring(0, width - 1) + "-");
                    word = word.Substring(width - 1);

                    strBuilder.Append(Environment.NewLine);
                }

                word = word.TrimStart();
            }
            strBuilder.Append(word);
            curLineLength += word.Length;
        }

        return strBuilder.ToString();
    }

    private static string[] Exp(string str, char[] splitChars)
    {
        List<string> parts = new List<string>();
        int startIndex = 0;
        while (true)
        {
            int index = str.IndexOfAny(splitChars, startIndex);

            if (index == -1) { parts.Add(str.Substring(startIndex)); return parts.ToArray(); }

            string word = str.Substring(startIndex, index - startIndex);
            char nextChar = str.Substring(index, 1)[0];
            if (char.IsWhiteSpace(nextChar))
            { parts.Add(word); parts.Add(nextChar.ToString());
            } else { parts.Add(word + nextChar); }

            startIndex = index + 1;
        }
    }

    /// END OF WORDWRAP CODE

    //TP time :))))
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} next> to go to the next page. Use <!{0} option 1/2/3> to choose that option if applicable. Use <!{0} skip> to skip until an option is presented. Use <!{0} rewind> to go back to the start of the module.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        if (command == "NEXT")
        {
            yield return null;
            if (scrolling)
            {
                Buttons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            Buttons[1].OnInteract();
        }
        else if (command == "REWIND")
        {
            yield return null;
            while (casePointer != 0)
            {
                Buttons[0].OnInteract();
                yield return new WaitForSeconds(0.2f);
            }
        }
        else if (command == "SKIP")
        {
            yield return null;
            while (!showingButtons)
            {
                Buttons[1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        else if (Regex.IsMatch(command, @"^OPTION\s[1-3]$"))
        {
            int option = command.Last() - '1';
            if (!showingButtons)
                yield return "sendtochaterror The buttons cannot be interacted with at this time.";
            else if (!IndivOptButtons[option].activeSelf)
                yield return "sendtochaterror The supplied button cannot be interacted with at this time.";
            else
            {
                yield return null;
                Buttons[option + 2].OnInteract();
            }
        }
    }
}
