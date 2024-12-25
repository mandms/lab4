using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Program
{
    static void Main(string[] args)
    {

        string inputPath = "input.csv";
        string outputPath = "out.csv";

        var lines = File.ReadAllLines(inputPath);
        var transitions = ParseInput(lines, out HashSet<string> finalStates, out List<string> alphabet);

        var (newTransitions, newStates, newFinalStates) = Determinize(transitions, finalStates, alphabet);

        WriteOutput(outputPath, newTransitions, newStates, newFinalStates, alphabet);
    }

    static Dictionary<string, Dictionary<string, HashSet<string>>> ParseInput(string[] lines, out HashSet<string> finalStates, out List<string> alphabet)
    {
        var transitions = new Dictionary<string, Dictionary<string, HashSet<string>>>();
        finalStates = new HashSet<string>();
        alphabet = new List<string>();

        var stateLine = lines[1].Split(';').Skip(1).ToList();
        var headerLine = lines[0].Split(';').Skip(1).ToList();

        for (int i = 0; i < headerLine.Count; i++)
        {
            if (headerLine[i] == "F")
                finalStates.Add(stateLine[i]);
        }

        for (int i = 2; i < lines.Length; i++)
        {
            var parts = lines[i].Split(';');
            var symbol = parts[0];

            if (symbol != "ε" && !alphabet.Contains(symbol))
                alphabet.Add(symbol);

            for (int j = 1; j < parts.Length; j++)
            {
                var state = stateLine[j - 1];
                if (!transitions.ContainsKey(state))
                    transitions[state] = new Dictionary<string, HashSet<string>>();

                if (!transitions[state].ContainsKey(symbol))
                    transitions[state][symbol] = new HashSet<string>();

                if (!string.IsNullOrEmpty(parts[j]))
                {
                    foreach (var target in parts[j].Split(','))
                    {
                        transitions[state][symbol].Add(target);
                    }
                }
            }
        }

        return transitions;
    }

    static (Dictionary<string, Dictionary<string, string>>, List<string>, HashSet<string>) Determinize(
        Dictionary<string, Dictionary<string, HashSet<string>>> transitions,
        HashSet<string> finalStates,
        List<string> alphabet)
    {
        var newTransitions = new Dictionary<string, Dictionary<string, string>>();
        var stateMapping = new Dictionary<HashSet<string>, string>(HashSet<string>.CreateSetComparer());
        var queue = new Queue<HashSet<string>>();

        // Начальное состояние
        var startState = EpsilonClosure(new HashSet<string> { transitions.Keys.First() }, transitions);
        stateMapping[startState] = "S0";
        queue.Enqueue(startState);

        var newStates = new List<string> { "S0" };
        var newFinalStates = new HashSet<string>();

        if (startState.Overlaps(finalStates))
            newFinalStates.Add("S0");

        int stateCounter = 1;

        while (queue.Count > 0)
        {
            var currentSet = queue.Dequeue();
            var currentStateName = stateMapping[currentSet];

            newTransitions[currentStateName] = new Dictionary<string, string>();

            foreach (var symbol in alphabet)
            {
                var targetSet = new HashSet<string>();

                foreach (var state in currentSet)
                {
                    if (transitions[state].ContainsKey(symbol))
                    {
                        targetSet.UnionWith(transitions[state][symbol]);
                    }
                }

                // Закрытие ε-переходов
                targetSet = EpsilonClosure(targetSet, transitions);

                if (targetSet.Count > 0)
                {
                    if (!stateMapping.ContainsKey(targetSet))
                    {
                        string newStateName = $"S{stateCounter++}";
                        stateMapping[targetSet] = newStateName;
                        newStates.Add(newStateName);

                        if (targetSet.Overlaps(finalStates))
                            newFinalStates.Add(newStateName);

                        queue.Enqueue(targetSet);
                    }

                    newTransitions[currentStateName][symbol] = stateMapping[targetSet];
                }
                else
                {
                    newTransitions[currentStateName][symbol] = "";
                }
            }
        }

        return (newTransitions, newStates, newFinalStates);
    }

    static HashSet<string> EpsilonClosure(HashSet<string> states, Dictionary<string, Dictionary<string, HashSet<string>>> transitions)
    {
        var closure = new HashSet<string>(states);
        var stack = new Stack<string>(states);

        while (stack.Count > 0)
        {
            var state = stack.Pop();

            if (transitions[state].ContainsKey("ε"))
            {
                foreach (var target in transitions[state]["ε"])
                {
                    if (!closure.Contains(target))
                    {
                        closure.Add(target);
                        stack.Push(target);
                    }
                }
            }
        }

        return closure;
    }

    static void WriteOutput(string outputPath, Dictionary<string, Dictionary<string, string>> transitions, List<string> states, HashSet<string> finalStates, List<string> alphabet)
    {
        using (var writer = new StreamWriter(outputPath))
        {
            writer.WriteLine($";{string.Join(";", finalStates.Select(s => "F"))}");
            writer.WriteLine($";{string.Join(";", states)}");

            foreach (var symbol in alphabet)
            {
                var line = new List<string> { symbol };

                foreach (var state in states)
                {
                    if (transitions[state].ContainsKey(symbol))
                        line.Add(transitions[state][symbol]);
                    else
                        line.Add("");
                }

                writer.WriteLine(string.Join(";", line));
            }
        }
    }
}
