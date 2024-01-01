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
import { Player } from "./enums.js";
import Expedition from "./expedition.js";
import { parseGameString } from "./gamestring.js";
import PlayerArea from "./playerarea.js";
import { parseCardColor, parseCardValue, parsePlayer } from "./schemas.js";
export default class LostVitiesViewer {
    constructor() {
        this._cards = [];
        this._deck = [];
        this._player1 = new PlayerArea(40, 60, 0);
        this._player2 = new PlayerArea(40, 0, 180);
        this._discard = new Map();
        this._turn = Player.NONE;
        this._actions = [];
        this.element = document.createElement("div");
        this.element.classList.add("lost-cities-viewer");
        this._deckCountLabel = document.createElement("span");
        this._deckCountLabel.classList.add("deck-count");
        this.element.appendChild(this._deckCountLabel);
        for (let i = 1; i <= 5; ++i) {
            const y = (i - 3) * 6;
            this._discard.set(i, new Expedition(72, 30 + y, 90));
        }
    }
    clear() {
        for (let card of this._cards) {
            card.element.remove();
        }
        this._player1.clear();
        this._player2.clear();
        for (let pile of this._discard.values()) {
            pile.clear();
        }
        this._cards.length = 0;
        this._deck.length = 0;
        this._updateDeckCount();
    }
    _updateDeckCount() {
        this._deckCountLabel.innerText = this._deck.length === 0 ? "" : this._deck.length.toString();
    }
    _createCard(data) {
        const card = new Card(parseCardColor(data.Color), parseCardValue(data.Value));
        this._cards.push(card);
        this.element.appendChild(card.element);
        card.setTransform(10, 30, -90);
        return card;
    }
    _loadPiles(source, target) {
        for (let key in source) {
            const color = parseCardColor(key);
            const pile = target.get(color);
            for (let cardData of source[key]) {
                pile.add(this._createCard(cardData));
            }
        }
    }
    loadFromReplayString(base64) {
        const parsed = parseGameString(base64);
        this.loadFromState(parsed.initialState);
        this._actions.length = 0;
        this._actions.push(...parsed.actions);
    }
    loadFromState(state) {
        this.clear();
        for (let cardData of state.Deck) {
            const card = this._createCard(cardData);
            this._deck.push(card);
            card.hidden = true;
            card.faceDown = true;
        }
        if (this._deck.length > 0) {
            this._deck[this._deck.length - 1].hidden = false;
        }
        this._loadPiles(state.Discarded, this._discard);
        this._loadPiles(state.Player1.Expeditions, this._player1.expeditions);
        this._loadPiles(state.Player2.Expeditions, this._player2.expeditions);
        for (let cardData of state.Player1.Hand) {
            this._player1.hand.add(this._createCard(cardData));
        }
        for (let cardData of state.Player2.Hand) {
            this._player2.hand.add(this._createCard(cardData));
        }
        this._turn = parsePlayer(state.CurrentPlayer);
        this._updateDeckCount();
    }
    loadFromResult(result) {
        this.loadFromState(result.InitialState);
        this._actions.length = 0;
        this._actions.push(...result.Actions);
    }
    nextAction() {
        if (this._actions.length === 0) {
            return;
        }
        const next = this._actions.shift();
        this.applyAction(next);
    }
    applyAction(action) {
        let actingPlayerArea;
        switch (this._turn) {
            case Player.PLAYER1:
                actingPlayerArea = this._player1;
                this._turn = Player.PLAYER2;
                break;
            case Player.PLAYER2:
                actingPlayerArea = this._player2;
                this._turn = Player.PLAYER1;
                break;
        }
        if ("playedIndex" in action) {
            action = {
                PlayedCard: actingPlayerArea.hand.get(action.playedIndex).data,
                Discarded: action.discarded,
                DrawnCard: action.drawnColor === 0
                    ? undefined
                    : this._discard.get(action.drawnColor).last.data
            };
        }
        if (actingPlayerArea != null) {
            const playedCard = actingPlayerArea.hand.remove(action.PlayedCard);
            if (action.Discarded) {
                this._discard.get(playedCard.color).add(playedCard);
            }
            else {
                actingPlayerArea.expeditions.get(playedCard.color).add(playedCard);
            }
            if (action.DrawnCard != null) {
                actingPlayerArea.hand.add(this._discard.get(parseCardColor(action.DrawnCard.Color)).remove(action.DrawnCard));
            }
            else {
                const drawnCard = this._deck.pop();
                drawnCard.faceDown = false;
                actingPlayerArea.hand.add(drawnCard);
                if (this._deck.length > 0) {
                    this._deck[this._deck.length - 1].hidden = false;
                }
                this._updateDeckCount();
            }
        }
        if (this._deck.length === 0) {
            this._turn = Player.NONE;
        }
    }
    delay(seconds) {
        return __awaiter(this, void 0, void 0, function* () {
            return new Promise(resolve => {
                setTimeout(resolve, seconds * 1000);
            });
        });
    }
}
