// IndexedDB avec un object store par type d'entité (approche relationnelle)
// Chaque store possède ses propres colonnes (= propriétés du modèle C#)
const DB_NAME = 'LemmatiseurDB';
const DB_VERSION = 5;
const STORE_NAMES = ['AnalyseResultat', 'CorpusData', 'ZipfItem', 'NGramModel'];

// Clé primaire de chaque store (doit correspondre à la propriété du modèle C#)
const STORE_KEY_PATHS = {
    'AnalyseResultat': 'id',
    'CorpusData': 'index',
    'ZipfItem': 'rang',
    'NGramModel': 'id'
};

// Historique des versions IndexedDB :
//   v1 : store unique "entities"
//   v2 : suppression de "entities", création des stores par type avec keyPath 'id'
//   v3 : (migration interne, stores inchangés)
//   v4 : version actuelle (stores : AnalyseResultat, CorpusData, ZipfItem, NGramModel)
//   v5 : keyPath distinct par store (index pour CorpusData, rang pour ZipfItem, id pour les autres)
function openDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = (event) => {
            const db = event.target.result;

            // Nettoyage de l'ancien store unique "entities" (v1 → v2+)
            if (event.oldVersion < 2) {
                if (db.objectStoreNames.contains('entities')) {
                    db.deleteObjectStore('entities');
                }
            }

            // Pour les versions ≤ 4, supprimer les stores existants pour recréer avec le bon keyPath
            if (event.oldVersion < 5) {
                for (const name of STORE_NAMES) {
                    if (db.objectStoreNames.contains(name)) {
                        db.deleteObjectStore(name);
                    }
                }
            }

            // Création d'un store par entité avec son keyPath spécifique
            for (const name of STORE_NAMES) {
                if (!db.objectStoreNames.contains(name)) {
                    db.createObjectStore(name, { keyPath: STORE_KEY_PATHS[name], autoIncrement: true });
                }
            }
        };
        request.onsuccess = (event) => resolve(event.target.result);
        request.onerror = (event) => reject(event.target.error);
    });
}

window.IndexedDbInterop = {
    // Récupère toutes les entrées d'un store
    async getAll(storeName) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readonly');
            const store = tx.objectStore(storeName);
            const request = store.getAll();
            request.onsuccess = () => {
                resolve(request.result.map(r => JSON.stringify(r)));
            };
            request.onerror = (e) => reject(e.target.error);
        });
    },

    // Récupère une entrée par son id (clé IndexedDB)
    async getById(storeName, id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readonly');
            const store = tx.objectStore(storeName);
            const request = store.get(Number(id));
            request.onsuccess = () => {
                resolve(request.result ? JSON.stringify(request.result) : null);
            };
            request.onerror = (e) => reject(e.target.error);
        });
    },

    // Insère ou remplace des éléments dans le store (upsert par clé)
    // itemsJson: tableau JSON des objets à stocker
    async add(storeName, itemsJson) {
        const db = await openDb();
        const items = JSON.parse(itemsJson);
        const keyPath = STORE_KEY_PATHS[storeName];
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            let count = 0;
            for (const item of items) {
                const obj = { ...item };
                // Si la clé = 0 ou undefined, on la retire pour auto-génération
                if (obj[keyPath] === 0 || obj[keyPath] === undefined) {
                    delete obj[keyPath];
                }
                // put = upsert : écrase si la clé existe, crée sinon
                const request = store.put(obj);
                request.onsuccess = () => count++;
                request.onerror = (e) => reject(e.target.error);
            }
            tx.oncomplete = () => resolve(count > 0);
            tx.onerror = (e) => reject(e.target.error);
        });
    },

    // Met à jour un élément (remplacement complet par clé id)
    async update(storeName, itemJson) {
        const db = await openDb();
        const item = JSON.parse(itemJson);
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            const request = store.put(item);
            request.onsuccess = () => resolve(true);
            request.onerror = (e) => reject(e.target.error);
        });
    },

    // Supprime un élément par son id
    async delete(storeName, id) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            const request = store.delete(Number(id));
            request.onsuccess = () => resolve(true);
            request.onerror = (e) => reject(e.target.error);
        });
    },

    // Supprime toutes les entrées d'un store
    async clear(storeName) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            const request = store.clear();
            request.onsuccess = () => resolve(true);
            request.onerror = (e) => reject(e.target.error);
        });
    },

    // Compte le nombre d'entrées d'un store
    async count(storeName) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readonly');
            const store = tx.objectStore(storeName);
            const request = store.count();
            request.onsuccess = () => resolve(request.result);
            request.onerror = (e) => reject(e.target.error);
        });
    }
};