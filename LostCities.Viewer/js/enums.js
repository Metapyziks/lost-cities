export var CardColor;
(function (CardColor) {
    CardColor[CardColor["RED"] = 1] = "RED";
    CardColor[CardColor["GREEN"] = 2] = "GREEN";
    CardColor[CardColor["BLUE"] = 3] = "BLUE";
    CardColor[CardColor["WHITE"] = 4] = "WHITE";
    CardColor[CardColor["YELLOW"] = 5] = "YELLOW";
    CardColor[CardColor["PURPLE"] = 6] = "PURPLE";
})(CardColor || (CardColor = {}));
export var CardValue;
(function (CardValue) {
    CardValue[CardValue["WAGER"] = 0] = "WAGER";
    CardValue[CardValue["TWO"] = 2] = "TWO";
    CardValue[CardValue["THREE"] = 3] = "THREE";
    CardValue[CardValue["FOUR"] = 4] = "FOUR";
    CardValue[CardValue["FIVE"] = 5] = "FIVE";
    CardValue[CardValue["SIX"] = 6] = "SIX";
    CardValue[CardValue["SEVEN"] = 7] = "SEVEN";
    CardValue[CardValue["EIGHT"] = 8] = "EIGHT";
    CardValue[CardValue["NINE"] = 9] = "NINE";
    CardValue[CardValue["TEN"] = 10] = "TEN";
})(CardValue || (CardValue = {}));
export const CARD_SYMBOLS = new Map([
    [CardColor.RED, "♥"],
    [CardColor.GREEN, "♠"],
    [CardColor.BLUE, "♣"],
    [CardColor.WHITE, "♦"],
    [CardColor.YELLOW, "★"],
    [CardColor.PURPLE, "⚘"]
]);
