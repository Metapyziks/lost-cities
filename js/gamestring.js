import { Player } from "./enums.js";
import { base64ToBytes, divideCards, readCardArray, readInt32 } from "./helpers.js";
export function parseGameString(base64) {
    let index = 0;
    const bytes = base64ToBytes(base64);
    const result = {
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
    const firstPlayer = bytes[index++];
    let deck;
    let discarded;
    let player1Expeditions;
    let player1Hand;
    let player2Expeditions;
    let player2Hand;
    [deck, index] = readCardArray(bytes, index);
    [discarded, index] = readCardArray(bytes, index);
    [player1Expeditions, index] = readCardArray(bytes, index);
    [player1Hand, index] = readCardArray(bytes, index);
    [player2Expeditions, index] = readCardArray(bytes, index);
    [player2Hand, index] = readCardArray(bytes, index);
    result.initialState = {
        CurrentPlayer: Player[firstPlayer].toUpperCase(),
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
    let actionCount;
    [actionCount, index] = readInt32(bytes, index);
    let actions = [];
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
