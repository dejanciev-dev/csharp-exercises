using System.ComponentModel;
using System.Globalization;
using CSharpExercises.Generics;
using CSharpExercises.Serialization;

namespace CSharpExercises.Tests;

public class GenericsTests
{
    private sealed class Recorder<T>(string name, List<string> log) : INotificationHandler<T> where T : Notification
    {
        public void Handle(T notification) => log.Add($"{name}: {notification.Recipient}");
    }

    private enum Priority
    {
        [Description("Can wait")] Low,
        Normal,
        [Description("Drop everything")] High,
        Urgent = High, // an alias: same value, second name
    }

    [Flags]
    private enum Permissions { None = 0, Read = 1, Write = 2 }

    [Fact]
    public void Bus_ABaseTypeHandlerReceivesDerivedNotifications()
    {
        List<string> log = [];
        var bus = new NotificationBus();
        bus.Register(new Recorder<Notification>("audit", log));
        bus.Register(new Recorder<SmsNotification>("sms gateway", log));
        bus.Register(new Recorder<UrgentSmsNotification>("pager", log));
        bus.Register(new Recorder<EmailNotification>("mailer", log));

        var delivered = bus.Publish(new UrgentSmsNotification("+389 70 000 000", "Server down"));

        Assert.Equal(3, delivered);
        Assert.Equal(["audit: +389 70 000 000", "sms gateway: +389 70 000 000", "pager: +389 70 000 000"], log);
    }

    [Fact]
    public void Bus_DispatchesOnTheRuntimeType()
    {
        List<string> log = [];
        var bus = new NotificationBus();
        bus.Register(new Recorder<EmailNotification>("mailer", log));

        Notification typedAsBase = new EmailNotification("ana@example.com", "Hi");

        Assert.Equal(1, bus.Publish(typedAsBase));
        Assert.Equal(["mailer: ana@example.com"], log);
    }

    [Fact]
    public void Bus_NotificationWithoutHandlers_ReachesNobody()
    {
        var bus = new NotificationBus();
        bus.Register(new Recorder<EmailNotification>("mailer", []));

        Assert.Equal(0, bus.Publish(new SmsNotification("+1", "hi")));
    }

    [Fact]
    public void Contravariance_LetsABaseHandlerStandInForADerivedOne()
    {
        INotificationHandler<Notification> general = new Recorder<Notification>("any", []);

        INotificationHandler<EmailNotification> forEmails = general; // compiles only because of `in T`

        Assert.Same(general, forEmails);
    }

    [Fact]
    public void Covariance_LetsDerivedSequencesBeUsedAsBaseSequences()
    {
        List<EmailNotification> emails = [new("a@x.com", "1"), new("b@x.com", "2"), new("a@x.com", "3")];

        Assert.Equal(["a@x.com", "b@x.com"], Variance.Recipients(emails));
        Assert.Equal("3", ((EmailNotification)Variance.Latest(emails)!).Subject);
    }

    [Fact]
    public void ArrayCovariance_IsCheckedOnlyAtRunTime()
    {
        // Arrays have been covariant since .NET 1.0, before generics, and the type check happens on every write.
        Notification[] notifications = new EmailNotification[1];

        Assert.Throws<ArrayTypeMismatchException>(() => notifications[0] = new SmsNotification("+1", "hi"));
    }

    [Fact]
    public void EnumInfo_ParsesNamesCaseInsensitively_ButNotNumbers()
    {
        Assert.True(EnumInfo<Priority>.TryParse(" high ", out var high));
        Assert.Equal(Priority.High, high);

        // Enum.TryParse happily accepts "42", a value the enum does not define.
        Assert.True(Enum.TryParse<Priority>("42", out var undefined));
        Assert.Equal((Priority)42, undefined);
        Assert.False(EnumInfo<Priority>.TryParse("42", out _));
        Assert.False(EnumInfo<Priority>.TryParse(null, out _));
    }

    [Fact]
    public void EnumInfo_DescribesValues()
    {
        Assert.Equal("Can wait", EnumInfo<Priority>.Describe(Priority.Low));
        Assert.Equal("Normal", EnumInfo<Priority>.Describe(Priority.Normal));
        Assert.Equal("Drop everything", EnumInfo<Priority>.Describe(Priority.Urgent)); // alias of High
        Assert.Equal("7", EnumInfo<Priority>.Describe((Priority)7));
    }

    [Fact]
    public void EnumInfo_KeepsSeparateDataPerEnumType()
    {
        Assert.False(EnumInfo<Priority>.IsFlags);
        Assert.True(EnumInfo<Permissions>.IsFlags);
        Assert.Equal([Permissions.None, Permissions.Read, Permissions.Write], EnumInfo<Permissions>.Values);
    }

    [Fact]
    public void ParseList_WorksForAnyIParsableType()
    {
        Assert.Equal([1, 2, 3], GenericParsing.ParseList<int>("1, 2,3"));
        Assert.Equal([new DateOnly(2026, 10, 27)], GenericParsing.ParseList<DateOnly>("2026-10-27", CultureInfo.InvariantCulture));
        Assert.Equal([1.5m, 2m], GenericParsing.ParseList<decimal>("1.5, 2", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void ParseList_WorksForOurOwnTypes()
    {
        var prices = GenericParsing.ParseList<Money>("9.99 EUR, 0.50 EUR");

        Assert.Equal(Money.Parse("10.49 EUR"), prices[0] + prices[1]);
    }

    [Fact]
    public void TryParseList_CollectsTheInvalidItems()
    {
        var (parsed, invalid) = GenericParsing.TryParseList<Guid>($"{Guid.Empty}, not-a-guid, , {Guid.Empty}");

        Assert.Equal(2, parsed.Count);
        Assert.Equal(["not-a-guid"], invalid);
    }
}
