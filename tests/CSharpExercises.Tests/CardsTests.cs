using CSharpExercises.Cards;

namespace CSharpExercises.Tests;

public class CardsTests
{
    [Fact]
    public void Card_ParsesCommonNotations()
    {
        Assert.Equal(new Card(14, 's'), Card.Parse("As"));
        Assert.Equal(new Card(10, 'h'), Card.Parse("10h"));
        Assert.Equal(new Card(10, 'd'), Card.Parse("TD"));
        Assert.Equal("Td", Card.Parse("10d").ToString());
        Assert.Throws<FormatException>(() => Card.Parse("1s"));
        Assert.Throws<FormatException>(() => Card.Parse("Kx"));
    }

    [Theory]
    [InlineData("As Ks Qs Js Ts", HandCategory.StraightFlush)]
    [InlineData("9c 9d 9h 9s 2d", HandCategory.FourOfAKind)]
    [InlineData("3c 3d 3h Ks Kd", HandCategory.FullHouse)]
    [InlineData("2h 7h 9h Jh Ah", HandCategory.Flush)]
    [InlineData("Ah 2c 3d 4s 5h", HandCategory.Straight)]
    [InlineData("7c 7d 7h Ks 2d", HandCategory.ThreeOfAKind)]
    [InlineData("Kc Kd 7h 7s 2d", HandCategory.TwoPair)]
    [InlineData("Jc Jd 7h 4s 2d", HandCategory.Pair)]
    [InlineData("Ac Jd 7h 4s 2d", HandCategory.HighCard)]
    [InlineData("Qc Kd Ah 2s 3d", HandCategory.HighCard)] // no wrapping around the ace
    public void PokerHand_Categorises(string hand, HandCategory expected)
    {
        Assert.Equal(expected, PokerHand.Evaluate(hand).Category);
    }

    [Fact]
    public void PokerHand_BreaksTiesByRankThenKickers()
    {
        Assert.True(PokerHand.Evaluate("2c 3d 4h 5s 6d") > PokerHand.Evaluate("Ac 2d 3h 4s 5d")); // the wheel is the lowest straight
        Assert.True(PokerHand.Evaluate("3c 3d 3h 2s 2d") > PokerHand.Evaluate("2c 2h 2s Ac Ad")); // trips decide a full house
        Assert.True(PokerHand.Evaluate("Kc Kd 7h 7s 3d") > PokerHand.Evaluate("Kh Ks 7c 7d 2c")); // kicker
        Assert.Equal(0, PokerHand.Evaluate("Ac Kd 9h 5s 3d").CompareTo(PokerHand.Evaluate("Ad Kh 9s 5c 3h"))); // split pot
        Assert.Throws<ArgumentException>(() => PokerHand.Evaluate("Ac Ac 9h 5s 3d"));
    }

    [Fact]
    public void PokerHand_FindsTheBestFiveOfSeven()
    {
        var board = "Th Jh Qh 2c 2d";

        var (flushDraw, cards) = PokerHand.Best(Card.ParseMany($"Ah Kh {board}"));
        var (pocketTwos, _) = PokerHand.Best(Card.ParseMany($"2h 2s {board}"));

        Assert.Equal(HandCategory.StraightFlush, flushDraw.Category);
        Assert.Equal([14], flushDraw.Tiebreakers);
        Assert.DoesNotContain(cards, c => c.Rank == 2);
        Assert.Equal(HandCategory.FourOfAKind, pocketTwos.Category);
        Assert.True(flushDraw > pocketTwos);
    }

    [Theory]
    [InlineData("As 6d", 17, true)]
    [InlineData("As 6d Th", 17, false)]
    [InlineData("As Ad 9c", 21, true)]
    [InlineData("Kc Qd 5h", 25, false)]
    [InlineData("As Ad Ac Ah", 14, true)]
    public void Blackjack_CountsAcesHighWhenTheyFit(string hand, int total, bool soft)
    {
        Assert.Equal((total, soft), Blackjack.Total(Card.ParseMany(hand)));
    }

    [Fact]
    public void Blackjack_DealerHitsSoft17OnlyUnderH17()
    {
        var hand = Card.ParseMany("As 6d");

        Assert.Equal(2, Blackjack.PlayDealer(hand, new Queue<Card>(Card.ParseMany("4c")), hitSoft17: false).Count);
        var hit = Blackjack.PlayDealer(hand, new Queue<Card>(Card.ParseMany("4c")), hitSoft17: true);
        Assert.Equal((21, true), Blackjack.Total(hit));
        Assert.True(Blackjack.IsBlackjack(Card.ParseMany("Ah Kd")));
        Assert.False(Blackjack.IsBlackjack(Card.ParseMany("7h 7d 7c")));
    }

    [Fact]
    public void Blackjack_DealerBustOddsMatchTheTables()
    {
        var six = Blackjack.DealerOutcomes(6);
        var ten = Blackjack.DealerOutcomes(10);

        Assert.Equal(1, six.Sum(), 9);
        Assert.Equal(0.4232, six[5], 4); // the dealer's weakest up card
        Assert.Equal(0.3536, Blackjack.DealerOutcomes(2)[5], 4);
        Assert.Equal(0.2121, ten[5], 4);
        Assert.True(Blackjack.DealerOutcomes(6, hitSoft17: true)[5] > six[5]);
    }
}
