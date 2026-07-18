using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Uses an L system to generate a sequence of roads, and then creates a mesh for each of them
/// </summary>

[CreateAssetMenu(fileName = "LSystem", menuName = "World/LSys")]
[System.Serializable]
public class LSystem : ScriptableObject
{
    public List<LRule> rules;
    public string axiom;
    public string finalString;
    [Space]
    public bool randomRuleOutput;
    [Space]
    public int iterations = 5;
    [Space]
    public int angle;
    public int m_length;
    //below the minimum the nodes combine, above the maximum connections are broken
    public Range<float> m_nodeLimitRange;

    HashSet<char> inputList;

    public void IterateTree()
    {
        string newString = "";
        foreach (char character in finalString)
        {
            if (!inputList.Contains(character))
            {
                newString += character;
            }
            else
            {
                foreach (LRule rule in rules)
                {
                    if (rule.input == character)
                    {
                        newString += rule.FetchOutput(randomRuleOutput);
                    }
                }
            }
        }

        finalString = newString;
    }

    public void GenerateSequence()
    {
        inputList = new HashSet<char>();
        foreach(LRule rule in rules) 
        {
            inputList.Add(rule.input);
        }

        finalString = axiom;

        for (int i = 0; i < iterations; i++)
        {
            IterateTree();
        }
    }
}

[System.Serializable]
public struct LRule 
{
    public char input;
    public string[] outputs;

    public LRule(char _in, string _out) 
    {
        input = _in;
        outputs = new string[]{_out};
    }

    public string Pass(string line, bool randomOutput) 
    {
        if (line.Contains(input.ToString()))
        {
            int index = randomOutput ? Random.Range(0, outputs.Length) : 0;
            line = line.Replace(input.ToString(), outputs[index]);
        }

        return line;
    }

    public string FetchOutput(bool randomOutput) 
    {
        int index = randomOutput ? Random.Range(0, outputs.Length) : 0;
        return outputs[index];
    }
}

public class LAgent
{
    public Vector3 position, tempPos, direction;
    public int length;

    public LAgent(Vector3 pos, Vector3 tPos, Vector3 dir, int len)
    {
        position = pos;
        direction = dir;
        tempPos = tPos;
        length = len;
    }
}

public enum Instructions
{
    DRAW = 'f',
    LEFT_TURN = '<',
    RIGHT_TURN = '>',
    SAVE = '[',
    LOAD = ']'
}
