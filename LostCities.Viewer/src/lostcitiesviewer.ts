import Card from "./card.js";
import { CardColor, CardValue } from "./enums.js";
import Hand from "./hand.js";

export default class LostVitiesViewer {
    readonly element: HTMLDivElement;

    private readonly _player1Hand = new Hand();
    private readonly _player2Hand = new Hand();

    constructor() {
        this.element = document.createElement("div");
        this.element.classList.add("lost-cities-viewer");

        this.element.appendChild(this._player1Hand.element);
        this.element.appendChild(this._player2Hand.element);

        this._player1Hand.element.classList.add("player1");
        this._player2Hand.element.classList.add("player2");

        this.fillHandsAsync();
    }

    async delay(seconds: number): Promise<void> {
        return new Promise(resolve => {
            setTimeout(resolve, seconds * 1000);
        });
    }

    async fillHandsAsync(): Promise<void> {
        const deck: Card[] = [];

        for (let i = 1; i <= 5; ++i) {
            deck.push(new Card(i, CardValue.WAGER));
            deck.push(new Card(i, CardValue.WAGER));
            deck.push(new Card(i, CardValue.WAGER));
            
            for (let j = 2; j <= 10; ++j) {
                deck.push(new Card(i, j));
            }
        }

        for (let i = 0; i < deck.length; ++i) {
            const j = i + Math.floor(Math.random() * (deck.length - i));
            [deck[i], deck[j]] = [deck[j], deck[i]];
        }

        for (let i = 0; i < 8; ++i) {
            this._player1Hand.add(deck.pop());
            this._player2Hand.add(deck.pop());
            await this.delay(0.5);
        }
    }
}
