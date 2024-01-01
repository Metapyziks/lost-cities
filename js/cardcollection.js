import Card from "./card.js";
import { parseCardColor, parseCardValue } from "./schemas.js";
export default class CardCollection {
    constructor(options) {
        this._cards = [];
        this._options = options;
    }
    add(card) {
        this._cards.push(card);
        if (this._options.sort) {
            this._cards.sort(Card.comparer);
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
        this._updateCardPositions();
        return card;
    }
    clear() {
        this._cards.length = 0;
    }
    _updateCardPositions() {
        var _a, _b, _c, _d, _e, _f, _g;
        const radius = (_a = this._options.radius) !== null && _a !== void 0 ? _a : 0;
        const angleIncrement = (_b = this._options.angleIncrement) !== null && _b !== void 0 ? _b : 0;
        const sin0 = Math.sin(this._options.angle * Math.PI / 180);
        const cos0 = Math.cos(this._options.angle * Math.PI / 180);
        const xAdd = cos0 * ((_c = this._options.xIncrement) !== null && _c !== void 0 ? _c : 0) - sin0 * ((_d = this._options.yIncrement) !== null && _d !== void 0 ? _d : 0);
        const yAdd = sin0 * ((_e = this._options.xIncrement) !== null && _e !== void 0 ? _e : 0) + cos0 * ((_f = this._options.yIncrement) !== null && _f !== void 0 ? _f : 0);
        for (let i = 0; i < this._cards.length; ++i) {
            const card = this._cards[i];
            card.element.style.zIndex = (((_g = this._options.offsetZ) !== null && _g !== void 0 ? _g : 0) + i).toString();
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
