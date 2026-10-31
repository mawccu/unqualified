using System.Collections.Generic;

/// <summary>
/// The console's phrasebook: general words and phrases for this job in each
/// language, the kind printed in any pocket phrasebook. It never translates what
/// the caller is saying; working a sentence out from it takes effort, which is the
/// point of claiming (or not claiming) a language. (It replaced a "phrase aid" that
/// printed the English of every customer line for free.)
/// </summary>
public static class Phrasebook
{
    static readonly Dictionary<string, (string term, string english)[]> Terms = new()
    {
        [CallLanguage.Spanish] = new[]
        {
            ("hola · buenas tardes", "hello · good afternoon"),
            ("pedido · número de pedido", "order · order number"),
            ("envío · mensajero", "delivery · courier"),
            ("no ha llegado", "it hasn't arrived"),
            ("dirección · piso", "address · flat"),
            ("luz · luces", "light · lights"),
            ("parpadea", "it blinks"),
            ("rojo · verde · azul", "red · green · blue"),
            ("rápido · lento", "fast · slow"),
            ("reembolso · recibo", "refund · receipt"),
            ("lo compré el...", "I bought it on..."),
            ("septiembre · octubre", "September · October"),
            ("un momento, por favor", "one moment, please"),
            ("gracias", "thank you"),
        },
        [CallLanguage.Arabic] = new[]
        {
            ("مرحبًا · مساء الخير", "hello · good evening"),
            ("طلب · رقم الطلب", "order · order number"),
            ("التوصيل · شركة الشحن", "delivery · courier"),
            ("لم يصل", "it hasn't arrived"),
            ("العنوان · الشقة", "address · flat"),
            ("ضوء · أضواء", "light · lights"),
            ("يومض", "it blinks"),
            ("أحمر · أخضر · أزرق", "red · green · blue"),
            ("بسرعة · ببطء", "fast · slowly"),
            ("استرداد المبلغ · الإيصال", "refund · receipt"),
            ("اشتريته في...", "I bought it on..."),
            ("سبتمبر · أكتوبر", "September · October"),
            ("لحظة من فضلك", "one moment, please"),
            ("شكرًا", "thank you"),
        },
    };

    /// The terms for a language (empty for English).
    public static IReadOnlyList<(string term, string english)> For(string language) =>
        Terms.TryGetValue(language, out var terms) ? terms : System.Array.Empty<(string, string)>();
}
