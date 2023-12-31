import Card from "./card.js";

export default class Hand {
    readonly element: HTMLDivElement;

    private readonly _cards: Card[] = [];
    
    constructor() {
        this.element = document.createElement("div");
        this.element.classList.add("hand");
    }

    add(card: Card): void {
        this._cards.push(card);
        this._cards.sort(Card.comparer);

        const index = this._cards.indexOf(card);

        if (index === this._cards.length - 1) {
            this.element.appendChild(card.element);
        } else {
            this._cards[index + 1].element.insertAdjacentElement("beforebegin", card.element);
        }

        this._updateCardPositions();
    }

    remove(card: Card): void {
        this._cards.splice(this._cards.indexOf(card), 1);
        this.element.removeChild(card.element);
        this._updateCardPositions();
    }

    private _updateCardPositions(): void {
        const radius = 64;
        const angleIncrement = 5;
        const cardWidth = 5.8;
        const cardHeight = 8.8;
        
        for (let i = 0; i < this._cards.length; ++i) {
            const card = this._cards[i];

            const relIndex = i - (this._cards.length - 1) * 0.5;
            const angle = relIndex * angleIncrement * Math.PI / 180;

            const sin = Math.sin(angle);
            const cos = Math.cos(angle);

            const x = sin * radius;
            const y = cos * radius - radius + cardHeight * 0.5;

            card.element.style.transform = `translate(${cardWidth * -0.5}em, ${cardHeight * -0.5}em) rotate(${angle}rad) translate(${x}em, ${y}em)`;
        }
    }
}