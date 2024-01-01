import Card from "./card.js";
import CardCollection from "./cardcollection.js";
import { CardColor, CardValue, Player } from "./enums.js";
import Expedition from "./expedition.js";
import PlayerArea from "./playerarea.js";
import { CardColorString, ICardData, IGameResultData, IGameStateData, IPlayerActionData, IResultFileData, parseCardColor, parseCardValue, parsePlayer } from "./schemas.js";

export default class LostVitiesViewer {
    readonly element: HTMLDivElement;

    private readonly _cards: Card[] = [];
    private readonly _deck: Card[] = [];
    private readonly _player1 = new PlayerArea(40, 60, 0);
    private readonly _player2 = new PlayerArea(40, 0, 180);
    private readonly _discard = new Map<CardColor, Expedition>();

    private readonly _deckCountLabel: HTMLSpanElement;

    private _turn: Player = Player.NONE;
    private _actions: IPlayerActionData[] = [];

    constructor() {
        this.element = document.createElement("div");
        this.element.classList.add("lost-cities-viewer");

        this._deckCountLabel = document.createElement("span");
        this._deckCountLabel.classList.add("deck-count");
        this.element.appendChild(this._deckCountLabel);

        this.element.addEventListener("paste", ev => this._onPaste(ev));
        document.addEventListener("keypress", ev => this._onKeyPress(ev));

        for (let i = 1; i <= 5; ++i) {
            const y = (i - 3) * 6;
            this._discard.set(i, new Expedition(72, 30 + y, 90));
        }
    }

    private _onPaste(ev: ClipboardEvent): void {
        const text = ev.clipboardData.getData("text");

        if (text == null) {
            return;
        }

        let result: IGameResultData;

        const obj = JSON.parse(text) as (IResultFileData | IGameResultData);

        if ("Results" in obj) {
            result = obj.Results[0];
        } else if ("InitialState" in obj) {
            result = obj;
        } else {
            return;
        }

        this.loadFromResult(result);
    }

    private _onKeyPress(ev: KeyboardEvent): void {
        if (ev.code === "Space") {
            this.nextAction();
        }
    }

    clear(): void {
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

    private _updateDeckCount(): void {
        this._deckCountLabel.innerText = this._deck.length === 0 ? "" : this._deck.length.toString();
    }

    private _createCard(data: ICardData): Card {
        const card = new Card(parseCardColor(data.Color), parseCardValue(data.Value));
        this._cards.push(card);
        this.element.appendChild(card.element);
        card.setTransform(10, 30, -90);

        return card;
    }

    private _loadPiles(source: { [key: string]: readonly ICardData[] }, target: Map<CardColor, CardCollection>): void {
        for (let key in source) {
            const color = parseCardColor(key as CardColorString);
            const pile = target.get(color);

            for (let cardData of source[key]) {
                pile.add(this._createCard(cardData));
            }
        }
    }

    loadFromState(state: IGameStateData): void {
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

    loadFromResult(result: IGameResultData): void {
        this.loadFromState(result.InitialState);

        this._actions.length = 0;
        this._actions.push(...result.Actions);
    }

    nextAction(): void {
        if (this._actions.length === 0) {
            return;
        }

        const next = this._actions.shift();
        this.applyAction(next);
    }

    applyAction(action: IPlayerActionData): void {
        let actingPlayerArea: PlayerArea;

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

        if (actingPlayerArea != null) {
            const playedCard = actingPlayerArea.hand.remove(action.PlayedCard);

            if (action.Discarded) {
                this._discard.get(playedCard.color).add(playedCard);
            } else {
                actingPlayerArea.expeditions.get(playedCard.color).add(playedCard);
            }

            if (action.DrawnCard != null) {
                actingPlayerArea.hand.add(this._discard.get(parseCardColor(action.DrawnCard.Color)).remove(action.DrawnCard));
            } else {
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

    async delay(seconds: number): Promise<void> {
        return new Promise(resolve => {
            setTimeout(resolve, seconds * 1000);
        });
    }
}
