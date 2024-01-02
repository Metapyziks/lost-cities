import Card from "./card.js";
import { parseCardColor, parseCardValue } from "./schemas.js";
export default class CardCollection {
    constructor(options) {
        this._cards = [];
        this._sortedCards = [];
        this._options = options;
    }
    get count() {
        return this._cards.length;
    }
    get last() {
        return this._cards.length === 0 ? undefined : this._cards[this._cards.length - 1];
    }
    get(index) {
        return this._cards[index];
    }
    add(card) {
        this._cards.push(card);
        this._sortedCards.push(card);
        if (this._options.sort) {
            this._sortedCards.sort(Card.comparer);
        }
        this._updateCardPositions();
    }
    insert(index, card) {
        this._cards.splice(index, 0, card);
        this._sortedCards.splice(index, 0, card);
        if (this._options.sort) {
            this._sortedCards.sort(Card.comparer);
        }
        this._updateCardPositions();
    }
    remove(card) {
        if (!(card instanceof Card)) {
            const color = parseCardColor(card.Color);
            const value = parseCardValue(card.Value);
            card = this._cards.find(x => x.color === color && x.value === value);
        }
        this._cards.splice(this._cards.indexOf(card), 1);
        this._sortedCards.splice(this._sortedCards.indexOf(card), 1);
        this._updateCardPositions();
        return card;
    }
    clear() {
        this._cards.length = 0;
        this._sortedCards.length = 0;
    }
    indexOf(card) {
        if (!(card instanceof Card)) {
            const color = parseCardColor(card.Color);
            const value = parseCardValue(card.Value);
            card = this._cards.find(x => x.color === color && x.value === value);
        }
        return this._cards.indexOf(card);
    }
    _updateCardPositions() {
        var _a, _b, _c, _d, _e, _f, _g;
        const radius = (_a = this._options.radius) !== null && _a !== void 0 ? _a : 0;
        const angleIncrement = (_b = this._options.angleIncrement) !== null && _b !== void 0 ? _b : 0;
        const sin0 = Math.sin(this._options.angle * Math.PI / 180);
        const cos0 = Math.cos(this._options.angle * Math.PI / 180);
        const xAdd = cos0 * ((_c = this._options.xIncrement) !== null && _c !== void 0 ? _c : 0) - sin0 * ((_d = this._options.yIncrement) !== null && _d !== void 0 ? _d : 0);
        const yAdd = sin0 * ((_e = this._options.xIncrement) !== null && _e !== void 0 ? _e : 0) + cos0 * ((_f = this._options.yIncrement) !== null && _f !== void 0 ? _f : 0);
        for (let i = 0; i < this._sortedCards.length; ++i) {
            const card = this._sortedCards[i];
            card.element.style.zIndex = (((_g = this._options.offsetZ) !== null && _g !== void 0 ? _g : 0) + i).toString();
            const relIndex = i - (this._sortedCards.length - 1) * 0.5;
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
