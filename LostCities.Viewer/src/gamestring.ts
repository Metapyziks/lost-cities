import { CardColor, Player } from "./enums.js";
import { base64ToBytes, divideCards, readCardArray, readInt32 } from "./helpers.js";
import { ICardData, IGameStateData, PlayerString } from "./schemas.js";

export interface ICompressedPlayerActionData {
    playedIndex: number;
    discarded: boolean;
    drawnColor: CardColor;
}

export interface IParsedGameString {
    version: number;
    colors: number;
    winner: Player;
    disqualified: Player;
    player1Score?: number;
    player2Score?: number;
    initialState: IGameStateData;
    actions: readonly ICompressedPlayerActionData[];
}

export function parseGameString(base64: string): IParsedGameString {
    let index = 0;

    const bytes = base64ToBytes(base64);
    const result: IParsedGameString = {
        version: bytes[index++],
        colors: bytes[index++],
        winner: bytes[index++],
        disqualified: bytes[index++],
        initialState: null,
        actions: null
    };

    if (result.disqualified !== Player.NONE) {
        [result.player1Score, index] = readInt32(bytes, index);
        [result.player2Score, index] = readInt32(bytes, index);
    }

    const firstPlayer = bytes[index++] as Player;

    let deck: readonly ICardData[];
    let discarded: readonly ICardData[];
    let player1Expeditions: readonly ICardData[];
    let player1Hand: readonly ICardData[];
    let player2Expeditions: readonly ICardData[];
    let player2Hand: readonly ICardData[];

    [deck, index] = readCardArray(bytes, index);
    [discarded, index] = readCardArray(bytes, index);
    [player1Expeditions, index] = readCardArray(bytes, index);
    [player1Hand, index] = readCardArray(bytes, index);
    [player2Expeditions, index] = readCardArray(bytes, index);
    [player2Hand, index] = readCardArray(bytes, index);

    result.initialState = {
        CurrentPlayer: Player[firstPlayer].toUpperCase() as PlayerString,
        Deck: deck,
        Discarded: divideCards(discarded, result.colors),
        Player1: {
            Seed: 0,
            Expeditions: divideCards(player1Expeditions, result.colors),
            Hand: player1Hand
        },
        Player2: {
            Seed: 0,
            Expeditions: divideCards(player2Expeditions, result.colors),
            Hand: player2Hand
        }
    };
    
    let actionCount: number;
    [actionCount, index] = readInt32(bytes, index);
    
    let actions: ICompressedPlayerActionData[] = [];
    for (let i = 0; i < actionCount; ++i) {
        const encoded = bytes[index++];
        actions.push({
            playedIndex: encoded & 0x7,
            discarded: (encoded & 0x8) === 0x8,
            drawnColor: encoded >> 4
        });
    }

    result.actions = actions;

    return result;
}

