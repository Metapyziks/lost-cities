import { CardColor, CardValue, Player } from "./enums.js";

export type CardColorString =
    | "Red"
    | "Green"
    | "Blue"
    | "White"
    | "Yellow"
    | "Purple";

export type CardValueString =
    | "Wager"
    | "Two"
    | "Three"
    | "Four"
    | "Five"
    | "Six"
    | "Seven"
    | "Eight"
    | "Nine"
    | "Ten";

export type PlayerString =
    | "None"
    | "Player1"
    | "Player2";

export function parseCardColor(str: CardColorString): CardColor {
    return CardColor[str.toUpperCase() as keyof typeof CardColor];
}

export function parseCardValue(str: CardValueString): CardValue {
    return CardValue[str.toUpperCase() as keyof typeof CardValue];
}

export function parsePlayer(str: PlayerString): Player {
    return Player[str.toUpperCase() as keyof typeof Player];
}

export interface ICardData {
    Color: CardColorString;
    Value: CardValueString;
}

export interface IPlayerViewData {
    Seed: number;
    DeckCount: number;
    Hand: readonly ICardData[];
    PlayerExpeditions: { [key: string]: readonly ICardData[] };
    OpponentExpeditions: { [key: string]: readonly ICardData[] };
    Discarded: { [key: string]: readonly ICardData[] };
    LastAction?: IPlayerActionData;
}

export interface IPlayerActionData {
    PlayedCard: ICardData;
    Discarded: boolean;
    DrawnCard?: ICardData;
}

export interface IGameStateData {
    CurrentPlayer: PlayerString;
    Player1: IPlayerStateData;
    Player2: IPlayerStateData;
    Deck: readonly ICardData[];
    Discarded: { [key: string]: readonly ICardData[] };
    LastAction?: IPlayerActionData;
}

export interface IPlayerStateData {
    Seed: number;
    Hand: readonly ICardData[];
    Expeditions: { [key: string]: readonly ICardData[] };
}

export interface IResultFileData {
    Summaries: readonly IGameSummaryData[];
    Results?: readonly IGameResultData[];
}

export interface IGameSummaryData {
    Config: IGameConfigData;
    FirstTurn: PlayerString;
    Winner: PlayerString;
    Disqualified: PlayerString;
    Player1Score?: number;
    Player2Score?: number;
}

export interface IGameConfigData {
    GameSeed: number;
    Player1Seed: number;
    Player2Seed: number;
}

export interface IGameResultData {
    InitialState: IGameStateData;
    FinalState: IGameStateData;
    Actions: readonly IPlayerActionData[];
    Disqualified: PlayerString;
}
