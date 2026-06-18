// IndexedDB avec un object store par type d'entité (approche relationnelle)
// Chaque store possède ses propres colonnes (= propriétés du modèle C#)
const DB_NAME = 'LemmatiseurDB';
const DB_VERSION = 2;
const STORE_NAMES = ['AnalyseResultat', 'CorpusData', 'ZipfItem', 'NGramModel'];

function openDb() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DB_NAME, DB_VERSION);
        request.onupgradeneeded = (event) => {
            const db = event.target.result;

            // Suppression de l'ancien store unique "entities" (v1)
            if (event.oldVersion < 2) {
                if (db.objectStoreNames.contains('entities')) {
                    db.deleteObjectStore('entities');
                }
            }

            // Création d'un store par entité, chacun avec ses colonnes
            for (const name of STORE_NAMES) {
                if (!db.objectStoreNames.contains(name)) {
                    db.createObjectStore(name, { keyPath: 'id', autoIncrement: true });
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

    // Insère des éléments dans le store
    // itemsJson: tableau JSON des objets à stocker
    async add(storeName, itemsJson) {
        const db = await openDb();
        const items = JSON.parse(itemsJson);
        return new Promise((resolve, reject) => {
            const tx = db.transaction(storeName, 'readwrite');
            const store = tx.objectStore(storeName);
            let count = 0;
            for (const item of items) {
                // Si id = 0, on le retire pour laisser IndexedDB auto-générer
                const obj = { ...item };
                if (obj.id === 0 || obj.id === undefined) {
                    delete obj.id;
                }
                const request = store.add(obj);
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