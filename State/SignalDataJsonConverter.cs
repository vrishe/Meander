using System.Collections;
using Meander.Signals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Meander.State;

internal sealed class SignalDataJsonConverter : JsonConverter<ISignalData>
{
    public override ISignalData ReadJson(JsonReader reader, Type objectType, ISignalData existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var dict = serializer.Deserialize<JObject>(reader);
        if (!dict.TryGetValue("Item1", StringComparison.OrdinalIgnoreCase, out var token))
            throw new InvalidOperationException($"Malformed JSON: cannot read {nameof(SignalKind)}.");

        var kind = token.ToObject<SignalKind>();
        switch (kind)
        {
            case SignalKind.Difference:
                return ReadDifferenceSignalData(dict);

            case SignalKind.Meander:
                return ReadMeanderSignalData(dict);
        }

        throw new NotSupportedException($"{kind} is not supported.");
    }

    public override void WriteJson(JsonWriter writer, ISignalData value, JsonSerializer serializer)
    {
        if (value == null) return;

        switch (value)
        {
            case DifferenceSignalData data:
                WriteDifferenceSignalData(data, writer, serializer);
                return;

            case MeanderSignalData data:
                WriteMeanderSignalData(data, writer, serializer);
                return;
        }

        throw new NotSupportedException($"{value.Kind} is not supported.");
    }

    private DifferenceSignalData ReadDifferenceSignalData(JObject dict)
    {
        return new DifferenceSignalData(dict.GetValue("Item2").ToObject<Guid>(),
            dict.GetValue("Item3").ToObject<Guid>());
    }

    private MeanderSignalData ReadMeanderSignalData(JObject dict)
    {
        return new MeanderSignalData(dict.GetValue("Item2").ToObject<double[]>());
    }

    private static void WriteDifferenceSignalData(DifferenceSignalData data, JsonWriter writer, JsonSerializer serializer)
    {
        serializer.Serialize(writer, (kind: data.Kind, first: data.MinuendSignalId, second: data.SubtrahendSignalId));
    }

    private static void WriteMeanderSignalData(MeanderSignalData data, JsonWriter writer, JsonSerializer serializer)
    {
        var values = new double[data.SamplesCount];
        for (int i = 0; i < values.Length; ++i)
            values[i] = data[i];

        serializer.Serialize(writer, (kind: data.Kind, values));
    }
}
