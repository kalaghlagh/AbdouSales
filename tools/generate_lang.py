# -*- coding: utf-8 -*-
# سكربت مساعد (لا يُشحن مع التطبيق) لتوليد Lang.ar.xaml و Lang.fr.xaml من مصدر ترجمة واحد.
import re

# (key, arabic, french)
ENTRIES = [
    ("S001", "إضافة زبون", "Ajouter un client"),
    ("S002", "إضافة / تعديل زبون", "Ajouter / modifier un client"),
    ("S003", "اسم الزبون", "Nom du client"),
    ("S004", "النشاط", "Activité"),
    ("S005", "العنوان", "Adresse"),
    ("S006", "الهاتف", "Téléphone"),
    ("S007", "رقم السجل التجاري", "N° du registre de commerce"),
    ("S008", "الرقم الجبائي", "Numéro fiscal (NIF)"),
    ("S009", "➕ حفظ زبون جديد", "➕ Enregistrer le client"),
    ("S010", "🗑 حذف المحدد", "🗑 Supprimer la sélection"),
    ("S011", "مسح الحقول", "Effacer les champs"),
    ("S012", "إغلاق", "Fermer"),
    ("S013", "إضافة سلعة", "Ajouter un article"),
    ("S014", "إضافة / تعديل سلعة (المخزون)", "Ajouter / modifier un article (stock)"),
    ("S015", "التعيين (اسم السلعة)", "Désignation (nom de l'article)"),
    ("S016", "الوحدة", "Unité"),
    ("S017", "قطعة", "Pièce"),
    ("S018", "كيس", "Sac"),
    ("S019", "رولو", "Rouleau"),
    ("S020", "متر", "Mètre"),
    ("S021", "متر مربع", "Mètre carré"),
    ("S022", "كغ", "Kg"),
    ("S023", "طن", "Tonne"),
    ("S024", "صندوق", "Carton"),
    ("S025", "سعر الوحدة (د.ج)", "Prix unitaire (DA)"),
    ("S026", "الرمز الشريطي (Barcode) — امسحه بالقارئ أو أدخله يدوياً", "Code-barres — scannez-le avec le lecteur ou saisissez-le manuellement"),
    ("S027", "🎲 توليد رمز", "🎲 Générer un code"),
    ("S028", "الكمية في المخزون", "Quantité en stock"),
    ("S029", "➕ حفظ سلعة جديدة", "➕ Enregistrer l'article"),
    ("S030", "التعيين", "Désignation"),
    ("S031", "سعر الوحدة", "Prix unitaire"),
    ("S032", "الرمز الشريطي", "Code-barres"),
    ("S033", "إضافة ممون", "Ajouter un fournisseur"),
    ("S034", "إضافة / تعديل ممون", "Ajouter / modifier un fournisseur"),
    ("S035", "اسم الممون", "Nom du fournisseur"),
    ("S036", "➕ حفظ ممون جديد", "➕ Enregistrer le fournisseur"),
    ("S037", "نقطة البيع (الكاشير)", "Point de vente (Caisse)"),
    ("S038", "🛒 نقطة البيع (الكاشير)", "🛒 Point de vente (Caisse)"),
    ("S039", "📷 امسح الباركود هنا (أو أدخله يدوياً ثم Enter)", "📷 Scannez le code-barres ici (ou saisissez-le puis Entrée)"),
    ("S040", "الكمية", "Quantité"),
    ("S041", "أو ابحث عن سلعة بالاسم", "Ou recherchez un article par nom"),
    ("S042", "🔦 التركيز على خانة المسح", "🔦 Revenir à la case de scan"),
    ("S043", "سلة المبيعات", "Panier de vente"),
    ("S044", "0 سلعة", "0 article"),
    ("S045", "السلعة", "Article"),
    ("S046", "المجموع (د.ج)", "Total (DA)"),
    ("S047", "زبون عابر", "Client de passage"),
    ("S048", "طريقة الدفع", "Mode de paiement"),
    ("S049", "نقدا", "Espèces"),
    ("S050", "بطاقة", "Carte"),
    ("S051", "شيك", "Chèque"),
    ("S052", "نسبة الرسم %", "Taux de TVA %"),
    ("S053", "المجموع بدون رسوم:", "Total hors taxes :"),
    ("S054", "القيمة المضافة:", "TVA :"),
    ("S055", "المجموع الكلي:", "Total TTC :"),
    ("S056", "المبلغ المستلم من الزبون (د.ج)", "Montant reçu du client (DA)"),
    ("S057", "الباقي:", "Monnaie rendue :"),
    ("S058", "✅ إتمام البيع وطباعة", "✅ Valider la vente et imprimer"),
    ("S059", "💾 حفظ بدون طباعة", "💾 Enregistrer sans imprimer"),
    ("S060", "🗑 إفراغ السلة", "🗑 Vider le panier"),
    ("S061", "سجل الفواتير", "Registre des factures"),
    ("S062", "سجل الفواتير المحفوظة", "Registre des factures enregistrées"),
    ("S063", "رقم الفاتورة", "N° de facture"),
    ("S064", "التاريخ", "Date"),
    ("S065", "الزبون", "Client"),
    ("S066", "المجموع الكلي (د.ج)", "Total TTC (DA)"),
    ("S067", "🖨 معاينة وطباعة الفاتورة المحددة", "🖨 Aperçu et impression de la facture sélectionnée"),
    ("S068", "📤 مشاركة الفاتورة", "📤 Partager la facture"),
    ("S069", "🗑 حذف الفاتورة المحددة", "🗑 Supprimer la facture sélectionnée"),
    ("S070", "معاينة الفاتورة", "Aperçu de la facture"),
    ("S071", "🎨 تغيير القالب", "🎨 Changer de modèle"),
    ("S072", "🖨 طباعة الفاتورة", "🖨 Imprimer la facture"),
    ("S073", "📤 مشاركة (PDF / واتساب / جيميل)", "📤 Partager (PDF / WhatsApp / Gmail)"),
    ("S074", "تسجيل الدخول", "Connexion"),
    ("S075", "اسم المستخدم", "Nom d'utilisateur"),
    ("S076", "كلمة المرور", "Mot de passe"),
    ("S077", "اسم المستخدم أو كلمة المرور غير صحيحة", "Nom d'utilisateur ou mot de passe incorrect"),
    ("S078", "دخول", "Se connecter"),
    ("S079", "تحرير فاتورة", "Modifier une facture"),
    ("S080", "قوالب الفاتورة", "Modèles de facture"),
    ("S081", "📄 سجل الفواتير", "📄 Registre des factures"),
    ("S082", "⚙ الإعدادات", "⚙ Paramètres"),
    ("S083", "اختر الزبون (أو أدخل بياناته يدوياً بالأسفل)", "Choisissez le client (ou saisissez ses informations ci-dessous)"),
    ("S084", "سطور الفاتورة", "Lignes de la facture"),
    ("S085", "➕ إضافة سطر من المخزون", "➕ Ajouter une ligne depuis le stock"),
    ("S086", "➕ سطر يدوي", "➕ Ligne manuelle"),
    ("S087", "🗑 حذف السطر المحدد", "🗑 Supprimer la ligne sélectionnée"),
    ("S088", "كيفية التسديد:", "Mode de paiement :"),
    ("S089", "تحويل بنكي", "Virement bancaire"),
    ("S090", "أجل", "À crédit"),
    ("S091", "نسبة الرسم %:", "Taux de TVA % :"),
    ("S092", "الطابع الجبائي:", "Timbre fiscal :"),
    ("S093", "💾 حفظ الفاتورة", "💾 Enregistrer la facture"),
    ("S094", "🖨 معاينة وطباعة", "🖨 Aperçu et impression"),
    ("S095", "إلغاء", "Annuler"),
    ("S096", "الإعدادات", "Paramètres"),
    ("S097", "إعدادات الشركة (تظهر في ترويسة الفاتورة المطبوعة)", "Paramètres de l'entreprise (affichés en en-tête de la facture imprimée)"),
    ("S098", "اسم الشركة / المسير", "Nom de l'entreprise / du gérant"),
    ("S099", "رقم المادة", "N° d'article (matricule fiscal)"),
    ("S100", "💾 حفظ", "💾 Enregistrer"),
    ("S101", "🎨 قالب الفاتورة الافتراضي", "🎨 Modèle de facture par défaut"),
    ("S102", "مشاركة الفاتورة", "Partager la facture"),
    ("S103", "📤 مشاركة الفاتورة بصيغة PDF", "📤 Partager la facture en PDF"),
    ("S104", "تم إنشاء ملف PDF للفاتورة:", "Le fichier PDF de la facture a été créé :"),
    ("S105", "📂 فتح مجلد الملف", "📂 Ouvrir le dossier du fichier"),
    ("S106", "👁 معاينة ملف PDF", "👁 Aperçu du fichier PDF"),
    ("S107", "💾 حفظ نسخة باسم آخر", "💾 Enregistrer une copie sous un autre nom"),
    ("S108", "رقم واتساب الزبون (اختياري)", "Numéro WhatsApp du client (facultatif)"),
    ("S109", "مثال: 0555123456 أو 213555123456", "Exemple : 0555123456 ou 213555123456"),
    ("S110", "البريد الإلكتروني للزبون (اختياري)", "E-mail du client (facultatif)"),
    ("S111", "اتركه فارغاً لاختيار المرسل إليه داخل جيميل", "Laissez vide pour choisir le destinataire dans Gmail"),
    ("S112", "📱 إرسال عبر واتساب", "📱 Envoyer via WhatsApp"),
    ("S113", "✉ إرسال عبر جيميل", "✉ Envoyer via Gmail"),
    ("S114", "كيف تتم المشاركة؟", "Comment souhaitez-vous partager ?"),
    ("S115", "📋 نسخ الملف إلى الحافظة فقط", "📋 Copier le fichier seulement"),
    ("S116", "🎨 اختر قالب الفاتورة", "🎨 Choisir un modèle de facture"),
    ("S117", "اختر التصميم الذي سيظهر عند طباعة أو تصدير الفواتير. يمكنك تغييره لاحقاً في أي وقت.", "Choisissez le design utilisé pour imprimer ou exporter vos factures. Vous pourrez le changer à tout moment."),
    ("S118", "فاتورة", "Facture"),
    ("S119", "معلومات الزبون", "Informations du client"),
    ("S120", "المجموع الكلي", "Total TTC"),
    ("S121", "كلاسيكي بسيط", "Classique sobre"),
    ("S122", "أبيض وأسود — اقتصادي وواضح عند الطباعة", "Noir et blanc — économique et net à l'impression"),
    ("S123", "احترافي كحلي", "Bleu marine professionnel"),
    ("S124", "التصميم الأصلي للبرنامج — رسمي وموثوق", "Le design d'origine de l'application — formel et fiable"),
    ("S125", "عصري أزرق ملكي", "Bleu royal moderne"),
    ("S126", "زوايا دائرية وإبراز المجموع الكلي — مظهر حديث", "Coins arrondis et total mis en valeur — look moderne"),
    ("S127", "🖈 اجعله القالب الافتراضي لكل الفواتير الجديدة", "🖈 En faire le modèle par défaut pour toutes les nouvelles factures"),
    ("S128", "✔ اعتماد هذا القالب", "✔ Valider ce modèle"),

    # ===== لوحة المعلومات (Dashboard) - جديد =====
    ("S129", "لوحة المعلومات", "Tableau de bord"),
    ("S130", "📊 لوحة المعلومات", "📊 Tableau de bord"),
    ("S131", "إجمالي المبيعات", "Chiffre d'affaires total"),
    ("S132", "إجمالي الأرباح", "Bénéfice total"),
    ("S133", "الفواتير غير المدفوعة", "Factures impayées"),
    ("S134", "المبلغ المستحق", "Montant dû"),
    ("S135", "سلع منخفضة المخزون", "Articles en stock faible"),
    ("S136", "قائمة المخزون (الكميات المتوفرة لكل سلعة)", "Inventaire (quantités disponibles par article)"),
    ("S137", "السعر", "Prix"),
    ("S138", "القيمة الإجمالية", "Valeur totale"),
    ("S139", "لا توجد فواتير غير مدفوعة حالياً 👍", "Aucune facture impayée actuellement 👍"),
    ("S140", "تعليم كمدفوعة", "Marquer comme payée"),
    ("S141", "🔄 تحديث", "🔄 Actualiser"),
    ("S142", "عدد الفواتير الإجمالي", "Nombre total de factures"),
    ("S143", "القيمة (د.ج)", "Valeur (DA)"),
    ("S144", "مدفوعة", "Payée"),
    ("S145", "غير مدفوعة", "Impayée"),
    ("S146", "الحالة", "Statut"),
    ("S147", "الفاتورة مدفوعة", "Facture payée"),
    ("S148", "لغة البرنامج", "Langue de l'application"),
    ("S149", "🛒 نقطة البيع (الكاشير)", "🛒 Point de vente (Caisse)"),
    ("S150", "🎯 آخر الفواتير غير المدفوعة", "🎯 Dernières factures impayées"),
    ("S151", "تم تعيين القالب الافتراضي لجميع الفواتير الجديدة.", "Le modèle par défaut a été appliqué à toutes les nouvelles factures."),
    ("S152", "نجاح", "Succès"),
    ("S153", "سعر التكلفة (د.ج)", "Prix d'achat (DA)"),
    ("S154", "اختر لغة واجهة البرنامج. يمكنك تغييرها في أي وقت من هنا أو من القائمة الرئيسية.", "Choisissez la langue de l'application. Vous pourrez la changer à tout moment ici ou depuis le menu principal."),
    ("S155", "اختر الممون (أو أدخل بياناته يدوياً بالأسفل) — تُملأ ترويسة الفاتورة تلقائياً", "Choisissez le fournisseur (ou saisissez ses informations ci-dessous) — l'en-tête de la facture se remplit automatiquement"),
    ("S156", "اختر ممونًا أولاً لعرض السلع المتوافقة مع نشاطه.", "Choisissez d'abord un fournisseur pour afficher les articles compatibles avec son activité."),
    ("S157", "لا توجد سلع مرتبطة بهذا الممون. اربط السلع بالممون من نافذة \"إضافة سلعة\".", "Aucun article n'est lié à ce fournisseur. Liez les articles au fournisseur depuis la fenêtre « Ajouter un article »."),
    ("S158", "الممونون الذين يوفرون هذه السلعة (حدّد واحداً أو أكثر)", "Fournisseurs de cet article (cochez-en un ou plusieurs)"),
    ("S159", "سعر الشراء لدى الممون", "Prix d'achat chez le fournisseur"),
    ("S160", "رمز السلعة عند الممون", "Code article chez le fournisseur"),
    ("S161", "📥 استيراد من Excel", "📥 Importer depuis Excel"),
    ("S162", "📄 تحميل نموذج Excel", "📄 Télécharger le modèle Excel"),
    ("S163", "سعر الشراء", "Prix d'achat"),
    ("S164", "الباركود", "Code-barres"),
    ("S165", "الممون", "Fournisseur"),
    ("S166", "اكتب الاسم الكامل", "Saisissez le nom complet"),
    ("S167", "مثال: مواد البناء", "Ex. : Matériaux de construction"),
    ("S168", "العنوان الكامل (الشارع، البلدية، الولاية)", "Adresse complète (rue, commune, wilaya)"),
    ("S169", "مثال: 0550 12 34 56", "Ex. : 0550 12 34 56"),
    ("S170", "مثال: 21/A/123456-00/11", "Ex. : 21/A/123456-00/11"),
    ("S171", "رقم التعريف الجبائي (15 رقماً)", "Numéro d'identification fiscale (15 chiffres)"),
    ("S172", "رقم المادة الجبائية", "Numéro d'article d'imposition"),
    ("S173", "امسح الرمز أو اكتبه أو ولّده", "Scannez, saisissez ou générez le code"),
    ("S174", "اسم السلعة كما سيظهر في الفاتورة", "Nom de l'article tel qu'il apparaîtra sur la facture"),
    ("S175", "تحديد", "Sél."),
    ("S176", "إضافة السلع المحددة لممون", "Ajouter les articles sélectionnés à un fournisseur"),
    ("S177", "اختر الممون", "Choisir le fournisseur"),
    ("S178", "نقل السلع (إزالة ارتباطها بالممونين الحاليين)", "Déplacer (retirer le lien avec les fournisseurs actuels)"),
    ("S179", "تنفيذ", "Valider"),
    ("S180", "إلغاء", "Annuler"),
    ("S181", "☑ تحديد الكل", "☑ Tout sélectionner"),
    ("S182", "☐ إلغاء التحديد", "☐ Tout désélectionner"),
    ("S183", "🔗 إضافة المحدد لممون آخر", "🔗 Ajouter la sélection à un autre fournisseur"),
    ("S184", "السلع المحددة:", "Articles sélectionnés :"),
    ("S185", "(الممون)", "(Fournisseur)"),
    ("S186", "(الزبون)", "(Client)"),
    ("S187", "المظهر", "Apparence"),
    ("S188", "إظهار / إخفاء العناصر", "Afficher / masquer"),
    ("S189", "معلومات الشركة", "Informations de l'entreprise"),
    ("S190", "مظهر البرنامج", "Thème de l'application"),
    ("S191", "اختر المظهر الفاتح أو الداكن. يُطبَّق فوراً على كل نوافذ البرنامج.", "Choisissez le thème clair ou sombre. Il s'applique immédiatement à toutes les fenêtres."),
    ("S192", "☀ فاتح", "☀ Clair"),
    ("S193", "🌙 داكن", "🌙 Sombre"),
    ("S194", "العناصر المحدَّدة تظهر في النافذة وغير المحدَّدة تُخفى. زر «الإعدادات» في القائمة الرئيسية لا يمكن إخفاؤه.", "Les éléments cochés sont affichés, les autres sont masqués. Le bouton « Paramètres » du menu principal ne peut pas être masqué."),
    ("S195", "إظهار الكل", "Tout afficher"),
    ("S196", "إخفاء الكل", "Tout masquer"),
    ("S197", "↺ إظهار كل العناصر في كل النوافذ", "↺ Tout réafficher dans toutes les fenêtres"),
    ("S198", "القائمة الرئيسية", "Menu principal"),
]

KEYS = {k: (ar, fr) for k, ar, fr in ENTRIES}
assert len(KEYS) == len(ENTRIES), "duplicate key!"

HEADER_AR = '''<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- لغة الواجهة: العربية (من اليمين إلى اليسار) -->
    <FlowDirection x:Key="AppFlowDirection">RightToLeft</FlowDirection>

'''
HEADER_FR = '''<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:sys="clr-namespace:System;assembly=mscorlib">

    <!-- Interface language: French (left to right) -->
    <FlowDirection x:Key="AppFlowDirection">LeftToRight</FlowDirection>

'''
FOOTER = "\n</ResourceDictionary>\n"

def xml_escape(s):
    return (s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
             .replace('"', "&quot;"))

def build(header, lang_index):
    lines = [header]
    for k, ar, fr in ENTRIES:
        val = ar if lang_index == 0 else fr
        lines.append(f'    <sys:String x:Key="{k}">{xml_escape(val)}</sys:String>\n')
    lines.append(FOOTER)
    return "".join(lines)

with open("Lang.ar.xaml", "w", encoding="utf-8") as f:
    f.write(build(HEADER_AR, 0))

with open("Lang.fr.xaml", "w", encoding="utf-8") as f:
    f.write(build(HEADER_FR, 1))

print("Generated", len(ENTRIES), "keys into Lang.ar.xaml and Lang.fr.xaml")
