var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
import Card from "./card.js";
import { CardValue } from "./enums.js";
import Hand from "./hand.js";
export default class LostVitiesViewer {
    constructor() {
        this._player1Hand = new Hand();
        this._player2Hand = new Hand();
        this.element = document.createElement("div");
        this.element.classList.add("lost-cities-viewer");
        this.element.appendChild(this._player1Hand.element);
        this.element.appendChild(this._player2Hand.element);
        this._player1Hand.element.classList.add("player1");
        this._player2Hand.element.classList.add("player2");
        this.fillHandsAsync();
    }
    delay(seconds) {
        return __awaiter(this, void 0, void 0, function* () {
            return new Promise(resolve => {
                setTimeout(resolve, seconds * 1000);
            });
        });
    }
    fillHandsAsync() {
        return __awaiter(this, void 0, void 0, function* () {
            const deck = [];
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
                yield this.delay(0.5);
            }
        });
    }
}
