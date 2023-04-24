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
    public SpriteRenderer[] DisplayOutlineSprites;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;
    private bool _moduleActivated;

    private static readonly string[] wateriswet = { "Atlantic Ocean", "Arctic Ocean", "Caribbean", "Pacific Ocean", "Southern Ocean", "Baltic Sea", "Indian Ocean", "Mediterranean", "Black Sea" };
    private static readonly string[] boats = { "Proa", "Catboat", "Lugger", "Snow", "Sloop", "Cutter", "Yawl", "Ketch", "Schooner", "Topsail Schooner", "Brig", "Schooner Brig", "Brigantine", "Barque", "Barquentine", "Polacre", "Fully Rigged Ship", "Junk", "Felucca", "Gunter", "Bilander" };
    private static readonly string alphabet = "ABCDEFGHIJKLMNOPQRSTU";

    private int chosenDisplayWord;
    private int chosenOutline;
    private int[] chosenDisplayBoats = new int[4];
    private string key = "";
    private int correctBoat;

    private static readonly char?[][] _octagons = new char?[2][]
    {
        new char?[16] {'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P'}, // outer
        new char?[16] {'Q', null, 'D', null, 'E', null, 'F', null, 'U', null, 'T', null, 'S', null, 'R', null} // inner
    };

    private enum OctagonSize
    {
        Outer,
        Inner
    }
    private enum Direction
    {
        Clockwise,
        Counterclockwise,
        Swap
    };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < 4; i++)
            Buttonage[i].OnInteract += ButtonagePrsesesed(i);
        for (int i = 0; i < 4; i++)
        {
            DisplayOutlineSprites[i].sprite = null;
            DisplayBoatSprites[i].sprite = null;
            Display.text = "";
        }
        Module.OnActivate += ActivateAndCalculate;
    }

    private void ActivateAndCalculate()
    {
        Audio.PlaySoundAtTransform("ss_activation", transform);

        chosenDisplayWord = Rnd.Range(0, 9);
        Display.text = wateriswet[chosenDisplayWord];
        Debug.LogFormat("[Setting Sail #{0}] The display reads: \"{1}\"", _moduleId, Display.text);

        key = GenerateKey();
        Debug.LogFormat("[Setting Sail #{0}] The generated key is {1}.", _moduleId, key);
        var num = CalculateTableTwo();
        var ternary = IntToTernaryString(num);
        Debug.LogFormat("[Setting Sail #{0}] The value calculated from Table B is {1}.", _moduleId, num);
        Debug.LogFormat("[Setting Sail #{0}] The instructions (ternary) are {1}.", _moduleId, ternary);

        GenerateAgain:
        chosenDisplayBoats = Enumerable.Range(0, 21).ToArray().Shuffle().Take(4).ToArray();
        chosenOutline = Rnd.Range(0, 4);
        for (int i = 0; i < 4; i++)
            DisplayOutlineSprites[i].sprite = i == chosenOutline ? Outline : null;

        var boatIndex = alphabet[chosenDisplayBoats[chosenOutline]];
        var currentOctagon = boatIndex >= 'A' && boatIndex <= 'P' ? OctagonSize.Outer : OctagonSize.Inner;
        var currentIndex = currentOctagon == OctagonSize.Outer ? Array.IndexOf(_octagons[0], boatIndex) : Array.IndexOf(_octagons[1], boatIndex);
        var instructions = ternary.Select(i => i == '0' ? Direction.Clockwise : i == '1' ? Direction.Counterclockwise : Direction.Swap).ToList();
        while (instructions.Count < 12)
            instructions.AddRange(instructions);
        var movementAmount = key.Select(i => i >= '0' && i <= '9' ? (i - '0') : ((i - 'A') % 10)).ToArray();
        var logArray = new string[12];
        for (int i = 0; i < 12; i++)
        {
            if (instructions[i] == Direction.Clockwise)
            {
                if (currentOctagon == OctagonSize.Outer)
                    currentIndex = (currentIndex + movementAmount[i]) % 16;
                else
                    currentIndex = (currentIndex + (2 * movementAmount[i])) % 16;
            }
            else if (instructions[i] == Direction.Counterclockwise)
            {
                if (currentOctagon == OctagonSize.Outer)
                    currentIndex = (((currentIndex - movementAmount[i]) % 16) + 16) % 16;
                else
                    currentIndex = (((currentIndex - (2 * movementAmount[i])) % 16) + 16) % 16;
            }
            else
            {
                if (currentOctagon == OctagonSize.Inner)
                    currentOctagon = OctagonSize.Outer;
                else
                {
                    currentOctagon = OctagonSize.Inner;
                    if (_octagons[(int)OctagonSize.Outer][currentIndex] == 'D')
                        currentIndex--;
                    if (_octagons[(int)OctagonSize.Outer][currentIndex] == 'F')
                        currentIndex++;
                    if (_octagons[(int)currentOctagon][currentIndex] == null)
                    {
                        if (movementAmount[i] % 2 == 0)
                            currentIndex = (currentIndex + 1) % 16;
                        else
                            currentIndex = (currentIndex + 15) % 16;
                    }
                }
            }
            logArray[i] = string.Format("[Setting Sail #{0}] After instruction {1} ({2}{3}), moved to node {4}.", _moduleId, i + 1, instructions[i], instructions[i] != Direction.Swap ? (" " + movementAmount[i]) : "", _octagons[(int)currentOctagon][currentIndex]);
        }

        var finalValue = _octagons[(int)currentOctagon][currentIndex] - 'A';

        if (!chosenDisplayBoats.Contains(finalValue.Value))
            goto GenerateAgain;

        // Success!
        _moduleActivated = true;
        for (int i = 0; i < 4; i++)
            DisplayBoatSprites[i].sprite = BoatSprites[chosenDisplayBoats[i]];
        Debug.LogFormat("[Setting Sail #{0}] Displayed boats: {1}", _moduleId, chosenDisplayBoats.Select(i => boats[i]).Join(", "));
        Debug.LogFormat("[Setting Sail #{0}] Boat #{1} is outlined.", _moduleId, chosenOutline + 1);
        Debug.LogFormat("[Setting Sail #{0}] Movement amounts: {1}", _moduleId, movementAmount.Join(""));
        Debug.LogFormat("[Setting Sail #{0}] Starting node: {1}", _moduleId, boatIndex);
        for (int i = 0; i < 12; i++)
            Debug.Log(logArray[i]);
        Debug.LogFormat("[Setting Sail #{0}] Landed at {1}.", _moduleId, (char)(finalValue + 'A'));
        correctBoat = Array.IndexOf(chosenDisplayBoats, finalValue);
        Debug.LogFormat("[Setting Sail #{0}] Press Boat #{1}.", _moduleId, correctBoat + 1);
    }

    private KMSelectable.OnInteractHandler ButtonagePrsesesed(int i)
    {
        return delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttonage[i].transform);
            Buttonage[i].AddInteractionPunch(0.5f);
            if (_moduleSolved || !_moduleActivated)
                return false;
            if (i == correctBoat)
            {
                Module.HandlePass();
                _moduleSolved = true;
                Audio.PlaySoundAtTransform("ss_solveSound", transform);
                Debug.LogFormat("[Setting Sail #{0}] Correctly pressed Boat #{1}. Module Solved!", _moduleId, i + 1);
            }
            else
            {
                Module.HandleStrike();
                Debug.LogFormat("[Setting Sail #{0}] Incorrectly pressed Boat #{1}. Strike.", _moduleId, i + 1);
            }
            return false;
        };
    }

    private int CalculateTableTwo()
    {
        int a = BombInfo.GetBatteryCount() * BombInfo.GetBatteryHolderCount();
        int b = BombInfo.GetPortCount() * BombInfo.GetPortPlateCount();
        int c = BombInfo.GetModuleNames().Count + (int)(BombInfo.GetTime() / 60);
        Debug.LogFormat("[Setting Sail #{0}] Variable A: {1}", _moduleId, a);
        Debug.LogFormat("[Setting Sail #{0}] Variable B: {1}", _moduleId, b);
        Debug.LogFormat("[Setting Sail #{0}] Variable C: {1}", _moduleId, c);

        switch (chosenDisplayWord)
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

    private string IntToTernaryString(int number)
    {
        var chars = "0123456789ABCDEF".ToCharArray();
        var str = new char[32]; // maximum number of chars in any base
        var i = str.Length;
        bool isNegative = (number < 0);
        if (number <= 0) // handles 0 and int.MinValue special cases
        {
            str[--i] = chars[-(number % 3)];
            number = -(number / 3);
        }
        while (number != 0)
        {
            str[--i] = chars[number % 3];
            number /= 3;
        }
        if (isNegative)
        {
            str[--i] = '-';
        }
        return new string(str, i, str.Length - i);
    }

    private static readonly string[][] _tableA = new string[3][]
    {
        new string[3] {"OBPKGLR2XUTH", "FI318SZQ4JDN", "596AVWY0MC7E" },
        new string[3] {"86YOLAI9GCDE", "0B32U1VFMTRN", "Z4Q5XJKSWP7H" },
        new string[3] {"XHP3LQYDUW56", "OC0KVJM2IFZT", "N1497RGEB8AS" }
    };

    private string GenerateKey()
    {
        var serialNumber = BombInfo.GetSerialNumber();
        string k = serialNumber;
        var ports = BombInfo.GetPorts();
        for (int i = 0; i < serialNumber.Length; i++)
        {
            int ixA = "ABCDEFGHIJKL".IndexOf(serialNumber[i]);
            int ixB = "MNOPQRSTUVWX".IndexOf(serialNumber[i]);
            int ixC = "YZ0123456789".IndexOf(serialNumber[i]);
            if (ixA != -1)
            {
                var ps2 = ports.Contains("PS2");
                var dvi = ports.Contains("DVI");
                if (ps2 != dvi)
                    k += _tableA[0][0][ixA];
                else if (ps2)
                    k += _tableA[0][1][ixA];
                else
                    k += _tableA[0][2][ixA];
            }
            else if (ixB != -1)
            {
                var rj = ports.Contains("RJ45");
                var ser = ports.Contains("Serial");
                if (rj != ser)
                    k += _tableA[1][0][ixB];
                else if (rj)
                    k += _tableA[1][1][ixB];
                else
                    k += _tableA[1][2][ixB];
            }
            else if (ixC != -1)
            {
                var parallel = ports.Contains("Parallel");
                var rca = ports.Contains("StereoRCA");
                if (parallel != rca)
                    k += _tableA[2][0][ixC];
                else if (parallel)
                    k += _tableA[2][1][ixC];
                else
                    k += _tableA[2][2][ixC];
            }
            else
            {
                throw new InvalidOperationException("Quinn Wuest fucked up");
            }
        }
        return k;
    }

    private bool inputIsValid(string cmd)
    {
        string[] validstuff = { "1", "2", "3", "4" };
        if (validstuff.Contains(cmd))
        {
            return true;
        }
        return false;
    }


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <#> [Presses that button in reading order]";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*button\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) || Regex.IsMatch(parameters[0], @"^\s*pos\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 2)
            {
                if (inputIsValid(parameters[1]))
                {
                    yield return null;
                    if (parameters[1].Equals("1"))
                    {
                        Buttonage[0].OnInteract();
                    }
                    else if (parameters[1].Equals("2"))
                    {
                        Buttonage[1].OnInteract();
                    }
                    else if (parameters[1].Equals("3"))
                    {
                        Buttonage[2].OnInteract();
                    }
                    else if (parameters[1].Equals("4"))
                    {
                        Buttonage[3].OnInteract();
                    }
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_moduleActivated)
            yield return true;
        Buttonage[correctBoat].OnInteract();
        yield return new WaitForSeconds(0.1f);
    }

}