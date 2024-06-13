static long GetTimestamp(string row) =>
    long.Parse(row.Split(',').First());

static bool ValidLine(string line) =>
    !string.IsNullOrWhiteSpace(line) && line.Contains(',');

static string Prompt(string text)
{
    Console.Write(text);
    return Console.ReadLine()!;
}

var inputEmotiBit = Prompt("Input folder EmotiBit: ");
var eventsEmotiBit = new List<(long t, string v)>();
foreach (var file in Directory.EnumerateFiles(inputEmotiBit!))
{
    if (file.EndsWith(".csv"))
    {
        Console.WriteLine($"Loading EmotiBit source file: {file}");
        foreach (var line in await File.ReadAllLinesAsync(file))
        {
            if (ValidLine(line))
            {
                eventsEmotiBit.Add((GetTimestamp(line), line));
            }
        }
    }
}

// Sorting by timestamp to find start and end faster.
eventsEmotiBit.Sort((a, b) => a.t.CompareTo(b.t));

var eventsUnity = new List<(long t, string s, string v)>();
while (true)
{
    var inputUnity = Prompt("Input folder Unity: ");
    foreach (var file in Directory.EnumerateFiles(inputUnity!))
    {
        if (file.EndsWith(".csv") || file.EndsWith(".txt"))
        {
            Console.WriteLine($"Parsing Unity source file: {file}");
            eventsUnity.Clear();
            foreach (var line in await File.ReadAllLinesAsync(file))
            {
                if (ValidLine(line))
                {
                    eventsUnity.Add((GetTimestamp(line), "Unity", line));
                }
            }

            // If we have at least one Unity event we can process it.
            if (eventsUnity.Count > 0)
            {
                var startUnity = eventsUnity.First()!.t;
                var endUnity = eventsUnity.Last()!.t;

                int startIndexEmotiBit = eventsEmotiBit.FindIndex(_ => _.t >= startUnity);
                int endIndexEmotiBit = eventsEmotiBit.FindLastIndex(_ => _.t <= endUnity);

                // If either one is -1 then the EmotiBit data falls outside the Unity span.
                if (startIndexEmotiBit != -1 && endIndexEmotiBit != -1)
                {
                    // Adding all the EmotiBit events.
                    eventsUnity.AddRange(
                        eventsEmotiBit
                            .Skip(startIndexEmotiBit)
                            .Take(endIndexEmotiBit - startIndexEmotiBit + 1)
                            .Select(_ => (_.t, "EmotiBit", _.v))
                    );

                    // Sorting by timestamp again to intertwice EmotiBit and Unity.
                    eventsUnity.Sort((a, b) => a.t.CompareTo(b.t));
                }

                // Write to output directory.
                await File.WriteAllLinesAsync($"{file[..^4]}-emotibit.csv", eventsUnity.Select(_ => $"{_.s},{_.v}"));
            }
        }
    }
}
