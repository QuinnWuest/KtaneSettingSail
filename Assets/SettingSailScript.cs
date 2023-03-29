using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Reflection;
using System.Runtime.InteropServices;

public class SettingSailScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] Buttonage;
    public TextMesh Display;
    public Sprite[] BoatSprites;
    public Sprite Outline;
    public SpriteRenderer[] DisplayBoatSprites;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private int the;

    string[] wateriswet = { "Atlantic Ocean", "Arctic Ocean", "Caribbean", "Pacific Ocean", "Southern Ocean", "Baltic Sea", "Indian Ocean", "Mediterranean", "Black Sea" };
    int[] displayboats = new int[4];
    string[] boats = { "Proa", "Catboat", "Lugger", "Snow", "Sloop", "Cutter", "Yawl", "Ketch", "Schooner", "Topsail Schooner", "Brig", "Schooner Brig", "Brigantine", "Barque", "Barquentine", "Polacre", "Fully Rigged Ship", "Junk", "Felucca", "Gunter", "Bilander" };
    string alphabet = "ABCDEFGHIJKLMNOPQRSTU";


    private void Start()
    {

        _moduleId = _moduleIdCounter++;

        foreach (KMSelectable button in Buttonage)
        {
            button.OnInteract += delegate () { inputPress(button); return false; };
        }



        the = Rnd.Range(0, 9);
        Display.text = wateriswet[the];

    tryagain:
        for (int i = 0; i < 4; i++)
        {
            displayboats[i] = Rnd.Range(0, 21);
        }
        if (displayboats.Distinct().Count() != 4)
        {
            goto tryagain;
        }
        // displayboats = Enumerable.Range(0, 21).ToArray().Shuffle().Take(4).ToArray(); 

        for (int i = 0; i < 4; i++)
        {
            DisplayBoatSprites[i].sprite = BoatSprites[displayboats[i]];
        }

        var num = tableTwo();
        Debug.Log(num);
        var ternary = IntToString(num);
        Debug.Log(ternary);
    }
    void inputPress(KMSelectable button)
    {
        button.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, button.transform);
    }
    void submitPress()
    {
        if (_moduleSolved == true)

        {
            Module.HandlePass();
            Audio.PlaySoundAtTransform("ss_solve", transform);
            Debug.LogFormat("[Setting Sail #{0}] Module Solved!", _moduleId);

        }
        else
        {
            Module.HandleStrike();
            Debug.LogFormat("[Setting Sail #{0}] Incorrect answer: {1}", _moduleId, Display.text);
        }


    }




    int tableTwo()
    {
        int a = (BombInfo.GetBatteryCount() * BombInfo.GetBatteryHolderCount());
        int b = (BombInfo.GetPortCount() * BombInfo.GetPortPlateCount());
        int c = BombInfo.GetModuleNames().Count + (int)(BombInfo.GetTime() / 60);

        switch (the)
        {
            case 0:
                return (a + b) * c;

            case 1:
            case 2:
            case 4:
            case 7:
                return a + b + c;

            case 3:
                return (a + c) * b;

            case 5:
                return a * b * c;

            case 6:
                return (b + c) * a;
            case 8:
                return new int[] { a, b, c }.Max();

            default:
                throw new InvalidOperationException("Invalid Number Path");
        }

        

    }

    public string IntToString(int a)
    {
        var chars = "0123456789ABCDEF".ToCharArray();
        var str = new char[32]; // maximum number of chars in any base
        var i = str.Length;
        bool isNegative = (a < 0);
        if (a <= 0) // handles 0 and int.MinValue special cases
        {
            str[--i] = chars[-(a % 3)];
            a = -(a / 3);
        }

        while (a != 0)
        {
            str[--i] = chars[a % 3];
            a /= 3;
        }

        if (isNegative)
        {
            str[--i] = '-';
        }

        return new string(str, i, str.Length - i);
    }
}