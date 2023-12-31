import Card from "./card.js";
import CardCollection from "./cardcollection.js";
import { CardColor, CardValue, Player } from "./enums.js";
import Expedition from "./expedition.js";
import { ICompressedPlayerActionData, parseGameString } from "./gamestring.js";
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
    private readonly _actions: (IPlayerActionData | ICompressedPlayerActionData)[] = [];
    private _actionIndex = 0;
    private readonly _undoStack: (() => void)[] = [];

    constructor() {
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

    loadFromReplayString(base64: string): void {
        const parsed = parseGameString(base64);
        this.loadFromState(parsed.initialState);

        this._actions.length = 0;
        this._actions.push(...parsed.actions);

        this._actionIndex = 0;
        this._undoStack.length = 0;
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
        
        this._actionIndex = 0;
        this._undoStack.length = 0;
    }

    nextAction(): boolean {
        if (this._actionIndex >= this._actions.length) {
            return false;
        }

        const next = this._actions[this._actionIndex++];
        this.applyAction(next);

        return true;
    }

    prevAction(): boolean {
        if (this._undoStack.length === 0 || this._actionIndex <= 0) {
            return false;
        }

        this._undoStack.pop()();
        this._actionIndex--;

        return true;
    }

    applyAction(action: IPlayerActionData | ICompressedPlayerActionData): void {
        let actingPlayerArea: PlayerArea;
        const prevTurn = this._turn;

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
                DrawnCard: action.drawnColor === 0 as CardColor
                    ? undefined
                    : this._discard.get(action.drawnColor).last.data
            };
        }

        if (actingPlayerArea != null) {
            const handIndex = actingPlayerArea.hand.indexOf(action.PlayedCard);
            const playedCard = actingPlayerArea.hand.remove(action.PlayedCard);
            const playedTo = action.Discarded
                ? this._discard.get(playedCard.color)
                : actingPlayerArea.expeditions.get(playedCard.color);

            const drawnFrom = action.DrawnCard != null
                ? this._discard.get(parseCardColor(action.DrawnCard.Color))
                : this._deck;

            playedTo.add(playedCard);

            let drawnCard: Card;

            if (drawnFrom instanceof CardCollection) {
                drawnCard = drawnFrom.remove(action.DrawnCard);
            } else {
                drawnCard = drawnFrom.pop();
                drawnCard.faceDown = false;

                if (drawnFrom.length > 0) {
                    drawnFrom[drawnFrom.length - 1].hidden = false;
                }
                
                this._updateDeckCount();
            }

            actingPlayerArea.hand.add(drawnCard);
            
            this._undoStack.push(() => {
                actingPlayerArea.hand.remove(drawnCard);

                if (drawnFrom instanceof CardCollection) {
                    drawnFrom.add(drawnCard);
                } else {
                    if (drawnFrom.length > 1) {
                        drawnFrom[drawnFrom.length - 2].hidden = true;
                    }
                    
                    drawnCard.faceDown = true;

                    drawnFrom.push(drawnCard);
                    drawnCard.setTransform(10, 30, -90);
                    drawnCard.element.style.zIndex = "0";
                    
                    this._updateDeckCount();
                }

                playedTo.remove(playedCard);
                actingPlayerArea.hand.insert(handIndex, playedCard);

                this._turn = prevTurn;
            });
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
