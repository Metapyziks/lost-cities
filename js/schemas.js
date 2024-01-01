import { CardColor, CardValue, Player } from "./enums.js";
export function parseCardColor(str) {
    return CardColor[str.toUpperCase()];
}
export function parseCardValue(str) {
    return CardValue[str.toUpperCase()];
}
export function parsePlayer(str) {
    return Player[str.toUpperCase()];
}
