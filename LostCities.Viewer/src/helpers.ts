import { CardColor, CardValue } from "./enums.js";
import { CardColorString, CardValueString, ICardData } from "./schemas.js";

export function rotate(x: number, y: number, angle: number): [x: number, y: number] {
    const rads = angle * Math.PI / 180;
    const cos = Math.cos(rads);
    const sin = Math.sin(rads);
    return [cos * x + sin * y, cos * y - sin * x];
}

export function base64ToBytes(base64: string): Uint8Array {
    const binString = atob(base64);
    return Uint8Array.from(binString, (m) => m.codePointAt(0));
}

export function readInt32(buffer: Uint8Array, index: number): [value: number, index: number] {
    let value = buffer[index + 0]
        | (buffer[index + 1] << 8)
        | (buffer[index + 2] << 16)
        | (buffer[index + 3] << 24);

    if (value >= 0x80000000) {
        value -= 0x80000000;
    }

    return [value, index + 4];
}

export function readCardArray(buffer: Uint8Array, index: number): [cards: readonly ICardData[], index: number] {
    const count = buffer[index++];
    const cards: ICardData[] = [];

    for (let i = 0; i < count; ++i) {
        const encoded = buffer[index++];
        const value = encoded & 0xf;
        const color = encoded >> 4;

        cards.push({
            Color: CardColor[color].toUpperCase() as CardColorString,
            Value: CardValue[value].toUpperCase() as CardValueString
        });
    }

    return [cards, index];
}

export function divideCards(cards: readonly ICardData[], colors: number): { [key: string]: readonly ICardData[] } {
    const result: { [key: string]: readonly ICardData[] } = {};

    for (var i = 1; i <= colors; ++i) {
        const key = CardColor[i] as CardColorString;
        const matching = cards.filter(x => x.Color === key);
        result[key] = matching;
    }

    return result;
}