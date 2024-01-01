export enum CardColor {
    RED = 1,
    GREEN,
    BLUE,
    WHITE,
    YELLOW,
    PURPLE
}

export enum CardValue {
    WAGER = 0,
    TWO = 2,
    THREE,
    FOUR,
    FIVE,
    SIX,
    SEVEN,
    EIGHT,
    NINE,
    TEN
}

export enum Player {
    NONE,
    PLAYER1,
    PLAYER2
}

export const CARD_SYMBOLS = new Map([
    [CardColor.RED, "♥"],
    [CardColor.GREEN, "♠"],
    [CardColor.BLUE, "♣"],
    [CardColor.WHITE, "♦"],
    [CardColor.YELLOW, "★"],
    [CardColor.PURPLE, "⚘"]
]);