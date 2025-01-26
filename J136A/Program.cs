List<ParsedLine> processingLines = [];
Dictionary<ushort, OutputLine> output = [];
Dictionary<char, ushort> Symbols = new()
{
    ['A'] = 2560,
    ['B'] = 2922,
    ['C'] = ushort.MaxValue,
    ['D'] = 2912,
    ['E'] = 2464,
    ['F'] = 3024,
    ['G'] = 3040,
    ['H'] = 2432,
    ['I'] = 2416,
    ['J'] = 2392,
    ['K'] = 2496,
    ['L'] = 2288,
    ['M'] = 2704,
    ['N'] = 2544,
    ['O'] = ushort.MaxValue,
    ['P'] = 3072,
    ['Q'] = 3104,
    ['R'] = 3264,
    ['S'] = 3136,
    ['T'] = 3128,
    ['U'] = 2304,
    ['V'] = 3211,
    ['W'] = 2272,
    ['X'] = 3056,
    ['Y'] = ushort.MaxValue,
    ['Z'] = 3120,
    ['/'] = ushort.MaxValue,
    ['='] = 2944,
    ['$'] = 501,
};

Dictionary<byte, ushort> Labels = [];

(ParsedLine?, EndState) ParseLine(string line)
{
    string address = line.Substring(13, 4);
    string leftCommand = line.Substring(18, 3);
    bool leftAddressInOctal = line.Substring(21, 1) == ",";
    string leftAddress = line.Substring(22, 4);
    string rightCommand = line.Substring(27, 3);
    bool rightAddressInOctal = line.Substring(30, 1) == ",";
    string rightAddress = line.Substring(31, 4);
    string terminator = line.Substring(35, 1);

    if (rightCommand == "   " || rightCommand == "---")
    {
        rightCommand = "000";
    }

    if (leftCommand == "   " || leftCommand == "---")
    {
        leftCommand = "000";
    }

    ushort addressValue;

    if (leftAddressInOctal)
    {
        ushort leftAddressValue = Convert.ToUInt16(leftAddress, 8);
        leftAddress = leftAddressValue.ToString();
    }
    else if (leftAddress[..1] == "$")
    {
        leftAddress = AssignAddress(leftAddress).ToString();
    }

    if (rightAddressInOctal)
    {
        ushort rightAddressValue = Convert.ToUInt16(rightAddress, 8);
        rightAddress = rightAddressValue.ToString();
    }
    else if (rightAddress[..1] == "$")
    {
        rightAddress = AssignAddress(rightAddress).ToString();
    }

    if (address == "   $")
    {
        Symbols['$'] = AssignAddress(rightAddress);
        return (null, EndState.None);
    }

    if (address[..1] == "*")
    {
        addressValue = Symbols['$']++;
        byte offset = byte.Parse(address[1..]);
        Labels[offset] = addressValue;
    }
    else if (Util.LabelRegex().Match(address[..1]).Success)
    {
        byte offset = byte.Parse(address[1..]);
        addressValue = (ushort)(Symbols[char.Parse(address[..1])] + offset);
    }
    else if (ushort.TryParse(address, out addressValue))
    {

    }
    else
    {
        addressValue = Symbols['$']++;
    }

    if (line.Substring(17, 1) == "+")
    {
        return ParseNumberLine(line, addressValue);
    }

    ushort leftCommandVal = Convert.ToUInt16(leftCommand, 8);
    ushort rightCommandVal = Convert.ToUInt16(rightCommand, 8);

    return (
        new(addressValue, leftCommandVal, leftAddress, rightCommandVal, rightAddress),
        terminator switch
        {
            "," => EndState.Next,
            "." => EndState.Done,
            _ => EndState.None,
        }
    );
}

(ParsedLine, EndState) ParseNumberLine(string line, ushort address)
{
    decimal mantissa = decimal.Parse(line.Substring(18, 12));
    byte exponent = byte.Parse(line.Substring(33, 2));

    //decimal value = mantissa * (decimal)Math.Pow(10, exponent);
    //string outputValue = Convert.ToString(value);

    return (new(address, 123, "4567", 123, "4567"), EndState.None);
}

void ResetLabels()
{
    foreach ((byte key, ushort _) in Labels)
    {
        Labels[key] = ushort.MaxValue;
    }
}

void FinalizeRoutine()
{
    foreach (ParsedLine line in processingLines)
    {
        if (output.ContainsKey(line.Address))
        {
            throw new Exception($"Duplicate line {line.Address}");
        }
        ushort leftAddress = AssignAddress(line.LeftAddress);
        ushort rightAddress = AssignAddress(line.RightAddress);
        output[line.Address] = new(
            line.Address,
            line.LeftCommand,
            leftAddress,
            line.RightCommand,
            rightAddress
        );
    }
    processingLines.Clear();
}

ushort AssignAddress(string address)
{
    if (ushort.TryParse(address, out ushort addressValue))
    {
        return addressValue;
    }
    else if (Util.LabelRegex().Match(address[..1]).Success)
    {
        ushort symbolValue = Symbols[char.Parse(address[..1])];
        if (symbolValue == ushort.MaxValue)
        {
            throw new Exception($"Using undefined symbol {address[..1]}");
        }
        return (ushort)(symbolValue + byte.Parse(address[1..]));
    }
    else if (address[..1] == "*")
    {
        ushort labelValue = Labels[byte.Parse(address[1..])];
        if (labelValue == ushort.MaxValue)
        {
            throw new Exception($"Using undefined label {address}");
        }
        return labelValue;
    }
    else
    {
        return 0;
    }
}

string[] lines = await File.ReadAllLinesAsync("../../../joss.txt");

foreach (string line in lines)
{
    (ParsedLine? parsedLine, EndState endState) = ParseLine(line.PadRight(36));
    if (parsedLine is not null)
    {
        processingLines.Add(parsedLine);
    }
    if (endState != EndState.None)
    {
        FinalizeRoutine();
        ResetLabels();
    }
}

Console.WriteLine(string.Join(
    '\n',
    output.Keys
        .Order()
        .Select(key => output[key])
        .Select(value => $"{Convert.ToString(value.Address, 8).PadLeft(4, '0')} {Convert.ToString(value.LeftCommand, 8).PadLeft(3, '0')} {Convert.ToString(value.LeftAddress, 8).PadLeft(4, '0')} {Convert.ToString(value.RightCommand, 8).PadLeft(3, '0')} {Convert.ToString(value.RightAddress, 8).PadLeft(4, '0')}")
));
record ParsedLine(
    ushort Address,
    ushort LeftCommand,
    string LeftAddress,
    ushort RightCommand,
    string RightAddress
);

record OutputLine(
    ushort Address,
    ushort LeftCommand,
    ushort LeftAddress,
    ushort RightCommand,
    ushort RightAddress
);

enum EndState
{
    None,
    Next,
    Done,
}
