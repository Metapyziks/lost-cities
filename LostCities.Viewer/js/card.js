import { CARD_SYMBOLS, CardColor, CardValue } from "./enums.js";
export default class Card {
    static comparer(a, b) {
        const colorCompare = a.color - b.color;
        if (colorCompare !== 0) {
            return colorCompare;
        }
        return a.value - b.value;
    }
    constructor(color, value) {
        this.color = color;
        this.value = value;
        this.element = document.createElement("div");
        this.element.classList.add("card");
        this.element.classList.add(CardColor[color].toLowerCase());
        this.label = document.createElement("span");
        this.label.classList.add("label");
        this.element.appendChild(this.label);
        this.symbol = document.createElement("span");
        this.symbol.classList.add("symbol");
        this.element.appendChild(this.symbol);
        if (value === CardValue.WAGER) {
            this.label.innerText = "W";
        }
        else {
            this.label.innerText = value.toString();
        }
        this.symbol.innerText = CARD_SYMBOLS.get(color);
    }
}
