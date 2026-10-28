/**
 * Kindergarten contact phones shown on the public screens (contact band and footer).
 * To change a number or add another one, edit this list only.
 *
 * `phone` is written in international format (+54 9 <area> <number>): it is shown as is
 * and the WhatsApp link is built from its digits, so it must include the country code
 * and the 9 for mobiles.
 */
export interface SchoolPhone {
    /** Text shown next to the number (visible to families, in Spanish). */
    label: string;
    phone: string;
}

export const SCHOOL_PHONES: readonly SchoolPhone[] = [
    // TODO: replace with the real morning-shift number.
    { label: "Turno mañana", phone: "+54 9 223 581-4289" },
    // TODO: replace with the real afternoon-shift number.
    { label: "Turno tarde", phone: "+54 9 223 581-4412" },
];

/** Click-to-chat link (https://wa.me/<digits>) for a phone in international format. */
export function whatsAppUrl(phone: string): string {
    return `https://wa.me/${phone.replace(/\D/g, "")}`;
}
