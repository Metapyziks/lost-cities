import { CARD_SYMBOLS, CardColor, CardValue } from "./enums.js";
export default class Card {
    static comparer(a, b) {
        const colorCompare = a.color - b.color;
        if (colorCompare !== 0) {
            return colorCompare;
        }
        return a.value - b.value;
    }
    get hidden() {
        return this.element.style.display === "none";
    }
    set hidden(value) {
        this.element.style.display = value ? "none" : "block";
    }
    get faceDown() {
        return this.element.classList.contains("face-down");
    }
    set faceDown(value) {
        this.element.classList.toggle("face-down", value);
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
    setTransform(x, y, angle) {
        this.element.style.transform = `translate(${x - Card.WIDTH * 0.5}em, ${y - Card.HEIGHT * 0.5}em) rotate(${angle}deg)`;
    }
}
Card.WIDTH = 5.8;
Card.HEIGHT = 8.8;
