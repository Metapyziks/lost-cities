import { CARD_SYMBOLS, CardColor, CardValue } from "./enums.js";
import { CardColorString, CardValueString, ICardData } from "./schemas.js";

export default class Card {
    static comparer(a: Card, b: Card): number {
        const colorCompare = a.color - b.color;
        if (colorCompare !== 0) {
            return colorCompare;
        }
        return a.value - b.value;
    }

    get hidden(): boolean {
        return this.element.style.display === "none";
    }

    set hidden(value: boolean) {
        this.element.style.display = value ? "none" : "block";
    }

    get faceDown(): boolean {
        return this.element.classList.contains("face-down");
    }

    set faceDown(value: boolean) {
        this.element.classList.toggle("face-down", value);
    }

    static readonly WIDTH = 5.8;
    static readonly HEIGHT = 8.8;

    readonly element: HTMLDivElement;
    readonly label: HTMLSpanElement;
    readonly symbol: HTMLSpanElement;

    readonly color: CardColor;
    readonly value: CardValue;

    get data(): ICardData {
        return {
            Color: CardColor[this.color] as CardColorString,
            Value: CardValue[this.value] as CardValueString
        };
    }
    
    constructor(color: CardColor, value: CardValue) {
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
        } else {
            this.label.innerText = value.toString();
        }

        this.symbol.innerText = CARD_SYMBOLS.get(color);
    }

    setTransform(x: number, y: number, angle: number): void {
        this.element.style.transform = `translate(${x - Card.WIDTH * 0.5}em, ${y - Card.HEIGHT * 0.5}em) rotate(${angle}deg)`;
    }
}
