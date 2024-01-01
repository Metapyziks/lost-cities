import Card from "./card.js";
import { ICardData, parseCardColor, parseCardValue } from "./schemas.js";

export interface ICardCollectionOptions {
    offsetX: number;
    offsetY: number;
    offsetZ?: number;
    angle: number;
    sort: boolean;

    radius?: number;
    angleIncrement?: number;
    xIncrement?: number;
    yIncrement?: number;
}

export default abstract class CardCollection {
    private readonly _cards: Card[] = [];
    
    private readonly _options: ICardCollectionOptions;

    constructor(options: ICardCollectionOptions) {
        this._options = options;
    }

    add(card: Card): void {
        this._cards.push(card);

        if (this._options.sort) {
            this._cards.sort(Card.comparer);
        }

        this._updateCardPositions();
    }

    remove(card: Card): Card;
    remove(card: ICardData): Card;
    remove(card: Card | ICardData): Card {
        if (!(card instanceof Card)) {
            const color = parseCardColor(card.Color);
            const value = parseCardValue(card.Value);
    
            card = this._cards.find(x => x.color === color && x.value === value);
        }

        this._cards.splice(this._cards.indexOf(card), 1);
        this._updateCardPositions();
        
        return card;
    }

    clear(): void {
        this._cards.length = 0;
    }

    private _updateCardPositions(): void {
        const radius = this._options.radius ?? 0;
        const angleIncrement = this._options.angleIncrement ?? 0;
    
        const sin0 = Math.sin(this._options.angle * Math.PI / 180);
        const cos0 = Math.cos(this._options.angle * Math.PI / 180);

        const xAdd = cos0 * (this._options.xIncrement ?? 0) - sin0 * (this._options.yIncrement ?? 0);
        const yAdd = sin0 * (this._options.xIncrement ?? 0) + cos0 * (this._options.yIncrement ?? 0);
        
        for (let i = 0; i < this._cards.length; ++i) {
            const card = this._cards[i];

            card.element.style.zIndex = ((this._options.offsetZ ?? 0) + i).toString();

            const relIndex = i - (this._cards.length - 1) * 0.5;
            const angle = this._options.angle + relIndex * angleIncrement;
            const angleRad = angle * Math.PI / 180;

            const sin = Math.sin(angleRad);
            const cos = Math.cos(angleRad);

            const x = sin * radius - sin0 * radius + xAdd * i;
            const y = cos0 * radius - cos * radius + yAdd * i;

            card.setTransform(this._options.offsetX + x, this._options.offsetY + y, angle);
        }
    }
}