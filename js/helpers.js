import { CardColor, CardValue } from "./enums.js";
export function rotate(x, y, angle) {
    const rads = angle * Math.PI / 180;
    const cos = Math.cos(rads);
    const sin = Math.sin(rads);
    return [cos * x + sin * y, cos * y - sin * x];
}
export function base64ToBytes(base64) {
    const binString = atob(base64);
    return Uint8Array.from(binString, (m) => m.codePointAt(0));
}
export function readInt32(buffer, index) {
    let value = buffer[index + 0]
        | (buffer[index + 1] << 8)
        | (buffer[index + 2] << 16)
        | (buffer[index + 3] << 24);
    if (value >= 0x80000000) {
        value -= 0x80000000;
    }
    return [value, index + 4];
}
export function readCardArray(buffer, index) {
    const count = buffer[index++];
    const cards = [];
    for (let i = 0; i < count; ++i) {
        const encoded = buffer[index++];
        const value = encoded & 0xf;
        const color = encoded >> 4;
        cards.push({
            Color: CardColor[color].toUpperCase(),
            Value: CardValue[value].toUpperCase()
        });
    }
    return [cards, index];
}
export function divideCards(cards, colors) {
    const result = {};
    for (var i = 1; i <= colors; ++i) {
        const key = CardColor[i];
        const matching = cards.filter(x => x.Color === key);
        result[key] = matching;
    }
    return result;
}
