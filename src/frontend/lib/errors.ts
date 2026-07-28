// Sprint 4 (Block B, T4): Bilingual error message dictionary.
// كل رسائل الخطأ في الواجهة تُعرض بالعربية + الإنجليزية (Frontend-first errors — soft rule #9).
// النمط الموحّد: { ar: "...", en: "..." } — يختار الـ component اللغة بناءً على locale.
//
// الاستخدام:
//   import { BilingualError, getBilingualError, mapApiError } from '@/lib/errors';
//   const e = getBilingualError(err, { ar: 'فشل التحميل', en: 'Failed to load' });
//   <ErrorState message={e} />
//
// الـ dictionary هنا للـ network / generic / common business errors.
// الـ specific validation messages تبقى في الـ form (validation form-specific).

import type { ApiError } from './api';

export interface BilingualError {
  ar: string;
  en: string;
}

const NETWORK_ERROR: BilingualError = {
  ar: 'تعذّر الاتصال بالخادم. تأكد من تشغيل الشبكة وحاول مرة أخرى.',
  en: 'Could not reach the server. Check your network and try again.',
};

const TIMEOUT_ERROR: BilingualError = {
  ar: 'انتهت مهلة الاتصال. الخادم بطيء أو غير متاح مؤقتاً.',
  en: 'Request timed out. The server is slow or temporarily unavailable.',
};

const UNAUTHORIZED_ERROR: BilingualError = {
  ar: 'انتهت صلاحية جلستك. يرجى تسجيل الدخول مرة أخرى.',
  en: 'Your session has expired. Please sign in again.',
};

const FORBIDDEN_ERROR: BilingualError = {
  ar: 'ليس لديك صلاحية لتنفيذ هذه العملية.',
  en: 'You do not have permission to perform this action.',
};

const NOT_FOUND_ERROR: BilingualError = {
  ar: 'العنصر المطلوب غير موجود. ربما تم حذفه أو نقله.',
  en: 'The requested item was not found. It may have been deleted or moved.',
};

const SERVER_ERROR: BilingualError = {
  ar: 'حدث خطأ في الخادم. حاول مرة أخرى لاحقاً.',
  en: 'A server error occurred. Please try again later.',
};

const VALIDATION_ERROR: BilingualError = {
  ar: 'البيانات المدخلة غير صحيحة. راجع الحقول وحاول مرة أخرى.',
  en: 'Invalid input data. Please review the fields and try again.',
};

const CONFLICT_ERROR: BilingualError = {
  ar: 'هذه العملية تتعارض مع حالة السجل الحالي. ربما تم تعديله من مستخدم آخر.',
  en: 'This action conflicts with the current state. The record may have been changed by another user.',
};

const UNKNOWN_ERROR: BilingualError = {
  ar: 'حدث خطأ غير متوقع. حاول مرة أخرى.',
  en: 'An unexpected error occurred. Please try again.',
};

/**
 * خريطة بأكواد HTTP الشائعة → BilingualError الافتراضية.
 * تُستخدم كـ fallback لو لم يأتِ الـ backend برسالة مخصصة.
 */
const STATUS_FALLBACKS: Record<number, BilingualError> = {
  400: VALIDATION_ERROR,
  401: UNAUTHORIZED_ERROR,
  403: FORBIDDEN_ERROR,
  404: NOT_FOUND_ERROR,
  408: TIMEOUT_ERROR,
  409: CONFLICT_ERROR,
  500: SERVER_ERROR,
  502: SERVER_ERROR,
  503: SERVER_ERROR,
  504: TIMEOUT_ERROR,
};

/**
 * شكل الـ Axios error (من غير استيراد axios لتجنب dependency cycle).
 */
interface AxiosLikeError {
  code?: string;
  response?: { status?: number; data?: ApiError };
  message?: string;
}

/**
 * يستخرج رسالة BilingualError من أي error.
 * الـ fallback يُستخدم لو لم نتمكن من استخراج رسالة مفيدة.
 *
 * الـ strategy:
 *  1. لو الـ error يحمل رسالة من الـ backend (response.data.detail / .message / .error) → نُرجعها.
 *     لو فقط AR أو EN متاح، نُضيف الـ placeholder في الجهة الأخرى.
 *  2. لو كود خطأ شبكة (ECONNABORTED / ERR_NETWORK) → NETWORK_ERROR أو TIMEOUT_ERROR.
 *  3. لو HTTP status معروف → الـ fallback المناسب من STATUS_FALLBACKS.
 *  4. خلاف ذلك → UNKNOWN_ERROR.
 */
export function mapApiError(e: unknown): BilingualError {
  const err = e as AxiosLikeError;

  // 1) Timeout (Axios يضع code = 'ECONNABORTED' عند timeout).
  if (err?.code === 'ECONNABORTED') {
    return TIMEOUT_ERROR;
  }

  // 2) Network error — Axios يضع code = 'ERR_NETWORK' على متصفحات حديثة.
  if (err?.code === 'ERR_NETWORK' || (err?.message === 'Network Error' && !err?.response)) {
    return NETWORK_ERROR;
  }

  // 3) HTTP response موجود — نحاول استخراج رسالة من body.
  const status = err?.response?.status;
  const data = err?.response?.data;

  if (status != null) {
    // 3a) لو الـ backend أعطى رسالة مفيدة، استعملها.
    const backendMsg = data?.detail || data?.message || data?.error;
    if (backendMsg && typeof backendMsg === 'string' && backendMsg.trim()) {
      return normalizeBackendMessage(backendMsg.trim());
    }
    // 3b) وإلا → الـ fallback بحسب الـ status.
    return STATUS_FALLBACKS[status] ?? UNKNOWN_ERROR;
  }

  // 4) Error.message موجود بدون response (نادر).
  if (err?.message && err.message !== 'Network Error') {
    return { ar: err.message, en: err.message };
  }

  return UNKNOWN_ERROR;
}

/**
 * يُحوّل رسالة الـ backend (التي عادةً تكون EN فقط في C#) إلى BilingualError
 * عبر وضع النص الإنجليزي في `en` واستخدام `ar` placeholder عام.
 *
 * لو اكتشفنا أن النص عربي بالفعل (يحتوي على حروف عربية) → نعتبره `ar` ونضع
 * `en` placeholder. هذا يحافظ على رسائل الـ backend اللي قد تكون localized.
 */
function normalizeBackendMessage(msg: string): BilingualError {
  if (hasArabic(msg)) {
    return { ar: msg, en: UNKNOWN_ERROR.en };
  }
  // الافتراضي: الـ backend يرجع EN → نُترجم الفئات الشائعة لو أمكن.
  const translated = COMMON_EN_TO_AR[msg.toLowerCase()];
  if (translated) {
    return { ar: translated, en: msg };
  }
  return { ar: SERVER_ERROR.ar, en: msg };
}

function hasArabic(s: string): boolean {
  return /[\u0600-\u06FF]/.test(s);
}

/**
 * خريطة لترجمة بعض الـ backend messages الإنجليزية الشائعة.
 * ليست شاملة — فقط الـ frequent errors. الـ الباقي يستعمل الـ fallback.
 */
const COMMON_EN_TO_AR: Record<string, string> = {
  'not found': 'العنصر غير موجود.',
  'unauthorized': 'غير مصرّح بالدخول.',
  'forbidden': 'العملية ممنوعة.',
  'invalid request': 'الطلب غير صالح.',
  'validation failed': 'فشل التحقق من البيانات.',
  'internal server error': 'خطأ في الخادم.',
  'conflict': 'تعارض في البيانات.',
  'duplicate': 'العنصر مكرر.',
  'cannot delete': 'لا يمكن الحذف — العنصر مرتبط بسجلات أخرى.',
  'cannot update': 'لا يمكن التعديل.',
  'amount must be positive': 'يجب أن يكون المبلغ أكبر من صفر.',
  'insufficient balance': 'الرصيد غير كافٍ.',
  'insufficient stock': 'الكمية في المخزون غير كافية.',
};

/**
 * helper للاستخدام في catch blocks:
 *
 *   try {
 *     await api.list();
 *   } catch (e) {
 *     const msg = getBilingualError(e, { ar: 'فشل تحميل القائمة', en: 'Failed to load list' });
 *     toast.error(`${msg.ar} / ${msg.en}`);
 *   }
 *
 * يحاول استخراج رسالة من الـ error أولاً، ثم يلجأ للـ overrideFallback.
 */
export function getBilingualError(e: unknown, overrideFallback?: BilingualError): BilingualError {
  const fromError = mapApiError(e);

  // لو الـ mapApiError رجّع UNKNOWN_ERROR (الـ generic)، نُرجع الـ override.
  if (fromError === UNKNOWN_ERROR && overrideFallback) {
    return overrideFallback;
  }
  return fromError;
}

/**
 * Format مختصر لعرض الرسالة — يدمج AR + EN في سطر واحد.
 * مفيد للـ toast (الذي يعرض سطر واحد فقط) ولـ page errors.
 */
export function formatBilingual(msg: BilingualError, separator = ' / '): string {
  // لو اللغتين متطابقتين (نادر)، نُرجع واحدة فقط.
  if (msg.ar === msg.en) return msg.ar;
  return `${msg.ar}${separator}${msg.en}`;
}
