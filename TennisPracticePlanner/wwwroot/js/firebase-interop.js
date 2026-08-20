// ES module loaded via Blazor JS isolation (IJSRuntime InvokeAsync<IJSObjectReference>("import", ...)).
// Wraps Firebase Auth (Google Sign-In) and Cloud Firestore for the cloud-sync feature.
// Guest Mode (local storage) does not use this module at all.

import { initializeApp } from "https://www.gstatic.com/firebasejs/12.18.0/firebase-app.js";
import {
    getAuth,
    GoogleAuthProvider,
    signInWithPopup,
    signOut,
    onAuthStateChanged as onFirebaseAuthStateChanged
} from "https://www.gstatic.com/firebasejs/12.18.0/firebase-auth.js";
import {
    getFirestore,
    doc,
    getDoc,
    setDoc,
    deleteDoc,
    collection,
    collectionGroup,
    getDocs,
    query,
    where
} from "https://www.gstatic.com/firebasejs/12.18.0/firebase-firestore.js";

const firebaseConfig = {
    apiKey: "AIzaSyBBsSwvp_gJGXWx6iCDZvUQMBn3Tq3taGI",
    authDomain: "tennis-practice-planner.firebaseapp.com",
    projectId: "tennis-practice-planner",
    storageBucket: "tennis-practice-planner.firebasestorage.app",
    messagingSenderId: "460305351055",
    appId: "1:460305351055:web:a5a77d92e31b1375e48bfa"
};

const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const db = getFirestore(app);
const googleProvider = new GoogleAuthProvider();

function toUserInfo(user) {
    if (!user) {
        return null;
    }

    return {
        uid: user.uid,
        email: user.email,
        displayName: user.displayName
    };
}

export function signInWithGoogle() {
    return signInWithPopup(auth, googleProvider).then(result => toUserInfo(result.user));
}

export function signOutUser() {
    return signOut(auth);
}

export function getCurrentUser() {
    return toUserInfo(auth.currentUser);
}

export function subscribeToAuthState(dotNetRef) {
    onFirebaseAuthStateChanged(auth, user => {
        dotNetRef.invokeMethodAsync("OnAuthStateChanged", toUserInfo(user));
    });
}

export async function isEmailAllowed(email) {
    if (!email) {
        return false;
    }

    const snapshot = await getDoc(doc(db, "config", "allowlist"));

    if (!snapshot.exists()) {
        return false;
    }

    const emails = snapshot.data().emails || [];
    return emails.some(allowed => allowed.toLowerCase() === email.toLowerCase());
}

export async function getUserInstructions(uid) {
    const snapshot = await getDocs(collection(db, "users", uid, "instructions"));
    return snapshot.docs.map(docSnapshot => docSnapshot.data());
}

export async function saveUserInstruction(uid, instructionJson) {
    const instruction = JSON.parse(instructionJson);
    await setDoc(doc(db, "users", uid, "instructions", instruction.id), instruction);
}

export async function deleteUserInstruction(uid, instructionId) {
    await deleteDoc(doc(db, "users", uid, "instructions", instructionId));
}

export async function getUserTemplates(uid) {
    const snapshot = await getDocs(collection(db, "users", uid, "templates"));
    return snapshot.docs.map(docSnapshot => docSnapshot.data());
}

export async function saveUserTemplate(uid, templateJson) {
    const template = JSON.parse(templateJson);
    await setDoc(doc(db, "users", uid, "templates", template.id), template);
}

export async function deleteUserTemplate(uid, templateId) {
    await deleteDoc(doc(db, "users", uid, "templates", templateId));
}

// Shared instructions live in each user's own "instructions" subcollection with isShared == true.
// A Firestore collection group query finds them across every user without a separate top-level collection.
export async function getSharedInstructions(tagFilter) {
    const instructionsGroup = collectionGroup(db, "instructions");
    const sharedQuery = tagFilter
        ? query(instructionsGroup, where("isShared", "==", true), where("tag", "==", tagFilter))
        : query(instructionsGroup, where("isShared", "==", true));

    const snapshot = await getDocs(sharedQuery);
    return snapshot.docs.map(docSnapshot => docSnapshot.data());
}
