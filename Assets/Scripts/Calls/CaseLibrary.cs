using System.Collections.Generic;

/// <summary>The languages a customer can speak. Ids are stable (saved); names are for the UI.</summary>
public static class CallLanguage
{
    public const string English = "en";
    public const string Spanish = "es";
    public const string Arabic = "ar";

    /// Languages with complete case content (the CV only offers these).
    public static readonly string[] Supported = { English, Spanish, Arabic };
    /// The ones a CV can claim on top of English.
    public static readonly string[] Claimable = { Spanish, Arabic };

    public static string Name(string id) => id switch
    {
        Spanish => "Spanish",
        Arabic => "Arabic",
        _ => "English",
    };

    public static string NativeName(string id) => id switch
    {
        Spanish => "Español",
        Arabic => "العربية",
        _ => "English",
    };

    public static bool RightToLeft(string id) => id == Arabic;
}

public enum CaseFamily { Delivery, Setup, Refund }

/// <summary>
/// One authored support case: the facts of the order (the same in every
/// language), what the customer says in each language, which questions reveal
/// which clue, the company's correct action, and what is merely a bluff. The
/// puzzle never depends on idioms: names, numbers, dates and products are plain.
/// </summary>
public sealed class CaseDefinition
{
    public string Id;
    public CaseFamily Family;
    /// Technical variants go to crew who claimed technical expertise.
    public bool Technical;
    public string Product;
    public string OrderId;
    /// What the company system shows for this order.
    public string Record;
    /// Customer name per language.
    public Dictionary<string, string> Names = new();
    /// The question that reveals this case's key clue, and the fact it records.
    public string DetailIntent;
    public string DetailFact;
    /// Facts the correct action needs first (e.g. the order looked up, the address confirmed).
    public string[] Requires = System.Array.Empty<string>();
    public string CorrectAction;
    /// The bluff for this family, and whether it happens to be true for this case.
    public string PromiseIntent;
    public bool PromiseSupported;
    /// Lines per key per language: greet, order, detail, good, bad, promise.
    public Dictionary<string, Dictionary<string, string>> Lines = new();

    public string Line(string key, string language)
    {
        if (Lines.TryGetValue(key, out var byLanguage) && byLanguage.TryGetValue(language, out var text))
            return text;
        return CommonLines.Line(key, language);
    }

    public string Name(string language) => Names.TryGetValue(language, out var n) ? n : Names[CallLanguage.English];
}

/// <summary>Things every customer says, whatever the case.</summary>
public static class CommonLines
{
    static readonly Dictionary<string, Dictionary<string, string>> Lines = new()
    {
        ["hold"] = L("Okay, I'll hold.", "Vale, espero.", "حسناً، سأنتظر."),
        ["hold_long"] = L("Hello? Are you still there?", "¿Hola? ¿Sigue ahí?", "ألو؟ هل ما زلت معي؟"),
        ["apology"] = L("Thank you, I appreciate that.", "Gracias, se lo agradezco.", "شكراً، أقدّر ذلك."),
        ["apology_again"] = L("Sorry doesn't fix it.", "Con disculpas no se arregla.", "الاعتذار لا يحلّ المشكلة."),
        ["transferred"] = L("Someone else? Fine, but please don't make me explain everything again.",
            "¿Otra persona? Bueno, pero no me haga repetirlo todo.",
            "شخص آخر؟ حسناً، لكن أرجو ألّا أضطرّ إلى إعادة كل شيء."),
        ["bounce"] = L("You're passing me around again?!", "¿Otra vez me están pasando de un lado a otro?", "هل تحوّلونني مرة أخرى؟!"),
        ["confirm_happy"] = L("No, that's everything. Thank you!", "No, eso es todo. ¡Gracias!", "لا، هذا كل شيء. شكراً لك!"),
        ["confirm_unresolved"] = L("Everything? You haven't fixed anything yet!", "¿Todo? ¡Si todavía no ha arreglado nada!", "كل شيء؟ لم تحلّ أي شيء بعد!"),
        ["hangup"] = L("Forget it. I'm done.", "Olvídelo. Ya me cansé.", "انسَ الأمر. لقد سئمت."),
        ["irrelevant"] = L("Why are you asking me that?", "¿Por qué me pregunta eso?", "لماذا تسألني عن ذلك؟"),
        ["who_is_that"] = L("Who are you talking to? Do you know how to do this or not?",
            "¿Con quién habla? ¿Sabe hacer esto o no?",
            "مع من تتحدّث؟ هل تعرف كيف تقوم بهذا أم لا؟"),
        ["already_said"] = L("I already told your colleague that.", "Eso ya se lo dije a su compañero.", "لقد أخبرتُ زميلك بذلك سابقاً."),
        ["escalated"] = L("Fine. Just make sure somebody actually calls me back.", "Bueno. Pero asegúrese de que alguien me llame de verdad.", "حسناً. لكن تأكّد أن أحداً سيتصل بي فعلاً."),
        ["ring_greeting"] = L("Hello? Is this support?", "¿Hola? ¿Es el servicio de atención?", "ألو؟ هل هذا قسم الدعم؟"),
    };

    static Dictionary<string, string> L(string en, string es, string ar) => new() { [CallLanguage.English] = en, [CallLanguage.Spanish] = es, [CallLanguage.Arabic] = ar };

    public static string Line(string key, string language)
    {
        if (Lines.TryGetValue(key, out var byLanguage))
            return byLanguage.TryGetValue(language, out var text) ? text : byLanguage[CallLanguage.English];
        return "";
    }
}

/// <summary>
/// The six authored cases (three families, a basic and a technical variant each)
/// and the company's own help pages. Spanish and Arabic were written for this
/// build and have not been reviewed by native speakers yet.
/// </summary>
public static class CaseLibrary
{
    static Dictionary<string, string> L(string en, string es, string ar) => new() { [CallLanguage.English] = en, [CallLanguage.Spanish] = es, [CallLanguage.Arabic] = ar };

    public static readonly CaseDefinition[] All =
    {
        new()
        {
            Id = "delivery_kettle", Family = CaseFamily.Delivery, Technical = false,
            Product = "Brewmatic kettle", OrderId = "48213",
            Record = "Order 48213 · Brewmatic kettle\nOrdered 21 Oct\nLabel created 22 Oct\nNOT collected by the courier (8 days)",
            Names = L("Pat Doyle", "Lucía Romero", "سلمى حداد"),
            DetailIntent = "Ask what the tracking page says", DetailFact = "tracking",
            Requires = new[] { "order_found" }, CorrectAction = "rebook_voucher",
            PromiseIntent = "Promise it arrives today", PromiseSupported = false,
            Lines = new()
            {
                ["greet"] = L("Hi, I ordered a kettle over a week ago and it still hasn't arrived.",
                    "Hola, pedí un hervidor hace más de una semana y todavía no ha llegado.",
                    "مرحباً، طلبتُ غلاية كهربائية منذ أكثر من أسبوع ولم تصل حتى الآن."),
                ["order"] = L("The order number is 48213.", "El número de pedido es 48213.", "رقم الطلب هو 48213."),
                ["detail"] = L("The tracking page just says 'label created'.", "La página de seguimiento solo dice «etiqueta creada».", "صفحة التتبّع تقول فقط «تم إنشاء الملصق»."),
                ["good"] = L("So it's finally being picked up? And a voucher too. Thank you!",
                    "¿Entonces por fin lo van a recoger? Y además un vale. ¡Gracias!",
                    "إذن سيُستلَم أخيراً؟ ومع قسيمة أيضاً. شكراً لك!"),
                ["bad"] = L("That doesn't sound right. It never even left your warehouse.",
                    "Eso no me cuadra. Ni siquiera ha salido de su almacén.",
                    "هذا غير منطقي. الطلب لم يغادر مستودعكم أصلاً."),
                ["promise"] = L("Today? Great, I'll stay in then.", "¿Hoy? Perfecto, entonces me quedo en casa.", "اليوم؟ ممتاز، سأبقى في البيت إذن."),
            },
        },
        new()
        {
            Id = "delivery_speaker", Family = CaseFamily.Delivery, Technical = true,
            Product = "EchoPod smart speaker", OrderId = "51907",
            Record = "Order 51907 · EchoPod smart speaker\nCollected 24 Oct\nHELD at Northgate depot\nReason: address incomplete (no flat number)",
            Names = L("Chris Okoye", "Javier Molina", "كريم منصور"),
            DetailIntent = "Ask them to confirm the full address", DetailFact = "address",
            Requires = new[] { "order_found", "address" }, CorrectAction = "release_depot",
            PromiseIntent = "Promise it arrives today", PromiseSupported = false,
            Lines = new()
            {
                ["greet"] = L("My speaker says it's 'held at depot'. What does that even mean?",
                    "Mi altavoz dice «retenido en el almacén». ¿Qué significa eso?",
                    "تقول صفحة الطلب إن السمّاعة «محتجزة في المستودع». ماذا يعني ذلك؟"),
                ["order"] = L("It's order 51907.", "Es el pedido 51907.", "رقم الطلب 51907."),
                ["detail"] = L("It's 14 Harbour Road, flat 3B.", "Es la calle del Puerto 14, piso 3B.", "العنوان: شارع الميناء 14، الشقة 3B."),
                ["good"] = L("Released? Brilliant, thanks for sorting the address.",
                    "¿Liberado? Genial, gracias por arreglar la dirección.",
                    "تمّ الإفراج عنها؟ رائع، شكراً لتصحيح العنوان."),
                ["bad"] = L("That's not it. It's sitting at your depot.", "No es eso. Está parado en su almacén.", "ليس هذا هو الحل. إنها موجودة في مستودعكم."),
                ["promise"] = L("Today? Even though it's stuck at a depot?", "¿Hoy? ¿Aunque esté atascado en un almacén?", "اليوم؟ مع أنها عالقة في المستودع؟"),
            },
        },
        new()
        {
            Id = "setup_plug", Family = CaseFamily.Setup, Technical = false,
            Product = "SparkPlug smart plug", OrderId = "60254",
            Record = "Order 60254 · SparkPlug smart plug\nDelivered 26 Oct\nNo open issues",
            Names = L("Sam Reyes", "Carmen Vidal", "ليلى عيسى"),
            DetailIntent = "Ask what the light is doing", DetailFact = "lights",
            Requires = new[] { "lights" }, CorrectAction = "pairing",
            PromiseIntent = "Promise an update will fix it tonight", PromiseSupported = false,
            Lines = new()
            {
                ["greet"] = L("Hi, my new smart plug won't connect to anything.", "Hola, mi enchufe inteligente nuevo no se conecta a nada.", "مرحباً، القابس الذكي الجديد لا يتصل بأي شيء."),
                ["order"] = L("Order 60254, but it arrived fine.", "Pedido 60254, pero llegó bien.", "الطلب رقم 60254، لكنه وصل سليماً."),
                ["detail"] = L("The little light is blinking blue, really fast.", "La lucecita parpadea en azul, muy rápido.", "الضوء الصغير يومض باللون الأزرق بسرعة كبيرة."),
                ["good"] = L("Oh! It's showing up in the app now. It works!", "¡Ah! Ya aparece en la aplicación. ¡Funciona!", "آه! ظهر الآن في التطبيق. إنه يعمل!"),
                ["bad"] = L("I did that. Still blinking blue.", "Ya lo hice. Sigue parpadeando en azul.", "فعلتُ ذلك. ما زال يومض بالأزرق."),
                ["promise"] = L("Tonight? I need it working now.", "¿Esta noche? Lo necesito funcionando ya.", "الليلة؟ أحتاجه يعمل الآن."),
            },
        },
        new()
        {
            Id = "setup_vacuum", Family = CaseFamily.Setup, Technical = true,
            Product = "DustBot 3 robot vacuum", OrderId = "60871",
            Record = "Order 60871 · DustBot 3 robot vacuum\nDelivered 19 Oct\nNo open issues",
            Names = L("Morgan Hale", "Diego Navarro", "عمر الخطيب"),
            DetailIntent = "Ask what the lights are doing", DetailFact = "lights",
            Requires = new[] { "lights" }, CorrectAction = "side_brush",
            PromiseIntent = "Promise an update will fix it tonight", PromiseSupported = false,
            Lines = new()
            {
                ["greet"] = L("My robot vacuum stops in the middle of the room and beeps.", "Mi robot aspirador se para en medio de la habitación y pita.", "المكنسة الآلية تتوقف في منتصف الغرفة وتُصدر صفيراً."),
                ["order"] = L("Order 60871.", "Pedido 60871.", "الطلب رقم 60871."),
                ["detail"] = L("The lights go red, red, then green, and it beeps twice.", "Las luces hacen rojo, rojo, luego verde, y pita dos veces.", "الأضواء: أحمر، أحمر، ثم أخضر، وتُصدر صفيرين."),
                ["good"] = L("There was a sock wrapped round the brush! It's moving again.", "¡Había un calcetín enrollado en el cepillo! Ya vuelve a moverse.", "كان هناك جورب ملتفّ حول الفرشاة! عادت تتحرّك."),
                ["bad"] = L("No, it's still stuck and beeping at me.", "No, sigue atascado y pitándome.", "لا، ما زالت عالقة وتصفّر."),
                ["promise"] = L("An update? It has a sock in it, I think.", "¿Una actualización? Creo que tiene un calcetín.", "تحديث؟ أظن أن فيها جورباً."),
            },
        },
        new()
        {
            Id = "refund_toaster", Family = CaseFamily.Refund, Technical = false,
            Product = "ToastMaster 2 toaster", OrderId = "37420",
            Record = "Order 37420 · ToastMaster 2 toaster\nBought 18 Oct (12 days ago)\nReturned unopened: eligible",
            Names = L("Alex Brennan", "Marta Suárez", "رنا العلي"),
            DetailIntent = "Ask for the receipt number", DetailFact = "receipt",
            Requires = new[] { "order_found", "receipt" }, CorrectAction = "refund_card",
            PromiseIntent = "Promise a full refund", PromiseSupported = true,
            Lines = new()
            {
                ["greet"] = L("I'd like to return a toaster. I never even opened it.", "Quiero devolver una tostadora. Ni siquiera la he abierto.", "أريد إرجاع محمصة خبز. لم أفتحها أصلاً."),
                ["order"] = L("Order 37420.", "Pedido 37420.", "الطلب رقم 37420."),
                ["detail"] = L("The receipt number is R-7730, from the 18th of October.", "El número del recibo es R-7730, del 18 de octubre.", "رقم الإيصال R-7730، بتاريخ 18 أكتوبر."),
                ["good"] = L("Refunded to my card? Perfect, thanks.", "¿Reembolsado a mi tarjeta? Perfecto, gracias.", "سيُعاد المبلغ إلى بطاقتي؟ ممتاز، شكراً."),
                ["bad"] = L("I just want my money back. It's unopened.", "Solo quiero mi dinero. Está sin abrir.", "أريد مالي فقط، فهي لم تُفتح."),
                ["promise"] = L("Great, as long as it actually happens.", "Genial, siempre que de verdad pase.", "جيد، بشرط أن يحدث ذلك فعلاً."),
            },
        },
        new()
        {
            Id = "refund_headphones", Family = CaseFamily.Refund, Technical = true,
            Product = "SoundNest Pro headphones", OrderId = "29918",
            Record = "Order 29918 · SoundNest Pro headphones\nBought 15 Sep (45 days ago)\nFault reported: left ear silent\n1-year warranty",
            Names = L("Jordan Pike", "Pablo Herrera", "يوسف ناصر"),
            DetailIntent = "Ask when they bought them", DetailFact = "date",
            Requires = new[] { "order_found", "date" }, CorrectAction = "warranty_swap",
            PromiseIntent = "Promise a full refund", PromiseSupported = false,
            Lines = new()
            {
                ["greet"] = L("My headphones stopped working in the left ear. I want my money back.",
                    "Mis auriculares dejaron de funcionar del lado izquierdo. Quiero que me devuelvan el dinero.",
                    "توقّفت السماعة اليسرى في سمّاعات الرأس عن العمل. أريد استرداد أموالي."),
                ["order"] = L("Order 29918.", "Pedido 29918.", "الطلب رقم 29918."),
                ["detail"] = L("I bought them on the 15th of September.", "Los compré el 15 de septiembre.", "اشتريتها في 15 سبتمبر."),
                ["good"] = L("A replacement pair? Fine, as long as both ears work.", "¿Unos de reemplazo? Vale, mientras funcionen los dos lados.", "زوج بديل؟ لا بأس، بشرط أن تعمل الأذنان معاً."),
                ["bad"] = L("That didn't go through, did it? I can hear you typing.", "Eso no ha funcionado, ¿verdad? Le oigo teclear.", "لم ينجح ذلك، أليس كذلك؟ أسمعك تكتب."),
                ["promise"] = L("A full refund? Great, I'll hold you to that.", "¿Reembolso completo? Genial, se lo voy a recordar.", "استرداد كامل؟ رائع، سأحاسبك على هذا الوعد."),
            },
        },
    };

    public static CaseDefinition Get(string id) => System.Array.Find(All, c => c.Id == id);

    /// The actions the company system offers, per family (id, button label).
    public static (string id, string label)[] Actions(CaseFamily family) => family switch
    {
        CaseFamily.Delivery => new[] { ("rebook_voucher", "Rebook courier + $5 voucher"), ("release_depot", "Release from depot"), ("reship", "Send a replacement"), ("refund_card", "Refund to card") },
        CaseFamily.Setup => new[] { ("pairing", "Tell them: add it in the app"), ("wifi", "Tell them: reconnect to Wi-Fi"), ("power_cycle", "Tell them: unplug and replug"), ("side_brush", "Tell them: clean the side brush"), ("wheels", "Tell them: check the wheels"), ("charge", "Tell them: charge it for 2 hours") },
        _ => new[] { ("refund_card", "Refund to card"), ("warranty_swap", "Warranty replacement"), ("store_credit", "Store credit"), ("reship", "Send a replacement") },
    };

    /// The company's own help pages: everything needed to solve every case is here.
    public static readonly (string title, string body)[] HelpPages =
    {
        ("Deliveries", "Label created, NOT collected for more than 48 hours: rebook the courier collection and add a $5 voucher.\n\nHeld at depot (address incomplete): confirm the full address with the customer, then Release from depot.\n\nNever promise a delivery date. The system cannot guarantee one."),
        ("Smart plug lights", "Blue, blinking fast: pairing mode. Tell them to add it in the app.\nBlue, blinking slowly: lost Wi-Fi. Tell them to reconnect to Wi-Fi.\nRed, steady: overload. Tell them to unplug and replug."),
        ("DustBot lights", "Red, red, green + two beeps: side brush jammed. Tell them to clean the side brush.\nRed, green, red: a wheel is stuck. Tell them to check the wheels.\nGreen, green, red: battery low. Tell them to charge it for 2 hours."),
        ("Refunds", "Within 30 days, with the receipt: refund to card.\nAfter 30 days: no refund. A fault within the 1-year warranty gets a warranty replacement.\nStore credit only if the customer asks for it."),
    };
}
