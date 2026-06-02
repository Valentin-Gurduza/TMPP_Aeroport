// Use var to allow Turbo to re-execute this script without redeclaration errors
var pendingBaggages = [];
var flaggedBaggages = [];
var clearanceQueue = [];
var isProcessingClearance = false;

window.isScanning = false;
window.isBeltRunning = false;

// Execute immediately on script evaluation (Turbo re-evaluates scripts on navigation)
fetchPendingBaggages();

async function fetchPendingBaggages() {
    try {
        const response = await fetch('/Security/GetPendingBaggages');
        const data = await response.json();
        
        pendingBaggages = data.filter(b => b.securityStatus === 'Pending');
        flaggedBaggages = data.filter(b => b.securityStatus === 'Flagged');
        
        renderConveyor();
        renderFlagged();
    } catch (error) {
        console.error("Error fetching baggages:", error);
        toastr.error("Failed to load conveyor belt.");
    }
}

function renderConveyor() {
    const container = document.getElementById("conveyor-belt");
    const emptyState = document.getElementById("conveyor-empty");
    const btnScan = document.getElementById("btn-scan");
    
    // Clear current (keep empty state hidden or visible later)
    container.innerHTML = '';
    
    if (pendingBaggages.length === 0) {
        container.appendChild(emptyState);
        emptyState.style.display = 'block';
        btnScan.disabled = true;
        btnScan.classList.add('opacity-50', 'cursor-not-allowed');
        return;
    }
    
    // Don't modify the toggle button here, just the empty state
    if (emptyState) emptyState.style.display = 'none';

    // Performance optimization: only render the first 15 bags to prevent browser lag and DOM overload
    const bagsToShow = pendingBaggages.slice(0, 15);
    bagsToShow.forEach((bag, index) => {
        const bagEl = document.createElement("div");
        bagEl.className = "bg-white py-4 rounded-xl shadow-md border-l-4 border-blue-500 flex-shrink-0 relative overflow-hidden conveyor-item entering";
        bagEl.id = `bag-${bag.id}`;
        bagEl.innerHTML = `
            <div class="text-xs text-slate-500 font-bold mb-1 w-[180px]">Flight ${bag.flightId || 'UNK'}</div>
            <div class="font-mono font-bold text-slate-800 dark:text-slate-200 text-lg mb-2">${bag.tagCode}</div>
            <div class="flex items-center text-xs text-slate-500">
                <span class="material-symbols-rounded text-[14px] mr-1">scale</span> ${bag.weight} kg
            </div>
        `;
        container.appendChild(bagEl);
        
        // Trigger reflow and start animation
        setTimeout(() => {
            bagEl.classList.remove('entering');
            bagEl.classList.add('moving');
        }, index * 150 + 50); // Faster, smoother entry stagger
    });
}

function renderFlagged() {
    const container = document.getElementById("flagged-queue");
    document.getElementById("flagged-count").innerText = flaggedBaggages.length;
    
    if (flaggedBaggages.length === 0) {
        container.innerHTML = '<div class="text-slate-500 italic text-center py-4">No flagged baggage.</div>';
        return;
    }

    container.innerHTML = '';
    flaggedBaggages.forEach(bag => {
        const bagEl = document.createElement("div");
        bagEl.className = "bg-white p-4 rounded-xl border border-red-200 flex items-center justify-between shadow-sm";
        bagEl.id = `flag-${bag.id}`;
        bagEl.innerHTML = `
            <div>
                <div class="font-mono font-bold text-red-700">${bag.tagCode}</div>
                <div class="text-xs text-slate-500">Weight: ${bag.weight} kg | Flight: ${bag.flightId || 'UNK'}</div>
            </div>
            <div class="flex space-x-2">
                <button onclick="manualInspect(${bag.id}, true)" class="px-3 py-1.5 bg-green-100 hover:bg-green-200 text-green-700 rounded text-xs font-bold transition" title="Clear Baggage">
                    Clear
                </button>
                <button onclick="manualInspect(${bag.id}, false)" class="px-3 py-1.5 bg-red-100 hover:bg-red-200 text-red-700 rounded text-xs font-bold transition" title="Confiscate">
                    Confiscate
                </button>
            </div>
        `;
        container.appendChild(bagEl);
    });
}

window.toggleBelt = function() {
    window.isBeltRunning = !window.isBeltRunning;
    const btnText = document.getElementById("text-toggle-belt");
    const btnIcon = document.getElementById("icon-toggle-belt");
    const btn = document.getElementById("btn-toggle-belt");

    if (window.isBeltRunning) {
        btnText.innerText = "Stop Belt";
        btnIcon.innerText = "stop";
        btn.classList.replace("bg-green-600", "bg-red-600");
        btn.classList.replace("hover:bg-green-700", "hover:bg-red-700");
        
        if (pendingBaggages.length > 0 && !window.isScanning) {
            scanBatch();
        }
    } else {
        btnText.innerText = "Start Belt";
        btnIcon.innerText = "play_arrow";
        btn.classList.replace("bg-red-600", "bg-green-600");
        btn.classList.replace("hover:bg-red-700", "hover:bg-green-700");
    }
}

async function scanBatch() {
    if (pendingBaggages.length === 0) return;
    if (window.isScanning) return;
    
    window.isScanning = true;

    // Batch processing size limit
    const batch = pendingBaggages.slice(0, 5);
    const idsToScan = batch.map(b => b.id);
    
    // Remove from local array so they aren't processed again
    pendingBaggages = pendingBaggages.filter(b => !idsToScan.includes(b.id));

    try {
        const response = await fetch('/Security/ScanBatch', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(idsToScan)
        });
        
        if (!response.ok) {
            throw new Error("Network response was not ok");
        }
    } catch (error) {
        console.error(error);
        toastr.error("Error starting scan.");
    } finally {
        window.isScanning = false;
        
        // If we're running low on rendered bags, fetch more silently
        if (pendingBaggages.length < 5) {
            setTimeout(fetchPendingBaggagesQuietly, 1500);
        }
    }
}

async function fetchPendingBaggagesQuietly() {
    try {
        const response = await fetch('/Security/GetPendingBaggages');
        const data = await response.json();
        const newPending = data.filter(b => b.securityStatus === 'Pending');
        
        const container = document.getElementById("conveyor-belt");
        let addedCount = 0;
        
        newPending.forEach(bag => {
            if (!document.getElementById(`bag-${bag.id}`)) {
                pendingBaggages.push(bag);
                addedCount++;
            }
        });
        
        if (addedCount > 0) {
            appendNewBagsToConveyor(addedCount);
        }
    } catch (error) {
        console.error("Quiet fetch failed", error);
    }
}

function appendNewBagsToConveyor(count) {
    const container = document.getElementById("conveyor-belt");
    const emptyState = document.getElementById("conveyor-empty");
    if (emptyState && pendingBaggages.length > 0) {
        emptyState.style.display = 'none';
    }
    
    const newBags = pendingBaggages.slice(-count);
    newBags.forEach((bag, index) => {
        const bagEl = document.createElement("div");
        bagEl.className = "bg-white py-4 rounded-xl shadow-md border-l-4 border-blue-500 flex-shrink-0 relative overflow-hidden conveyor-item entering";
        bagEl.id = `bag-${bag.id}`;
        bagEl.innerHTML = `
            <div class="text-xs text-slate-500 font-bold mb-1 w-[180px]">Flight ${bag.flightId || 'UNK'}</div>
            <div class="font-mono font-bold text-slate-800 dark:text-slate-200 text-lg mb-2">${bag.tagCode}</div>
            <div class="flex items-center text-xs text-slate-500">
                <span class="material-symbols-rounded text-[14px] mr-1">scale</span> ${bag.weight} kg
            </div>
        `;
        container.appendChild(bagEl);
        
        setTimeout(() => {
            bagEl.classList.remove('entering');
            bagEl.classList.add('moving');
        }, index * 150 + 50);
    });
}

// Automatic Scanning Loop (runs only if belt is started)
if (window.scannerInterval) clearInterval(window.scannerInterval);
window.scannerInterval = setInterval(() => {
    if (window.isBeltRunning && pendingBaggages.length > 0 && !window.isScanning) {
        scanBatch();
    }
}, 4000);

// Wire SignalR handler safely — retry until globalConnection is established
function wireSecuritySignalR() {
    if (window.globalConnection && window.globalConnection.state === 'Connected') {
        window.globalConnection.off("ScannerStatus"); // Prevent duplicate listeners from Turbo
        window.globalConnection.on("ScannerStatus", function (data) {
            const bagEl = document.getElementById(`bag-${data.id}`);
            if (!bagEl) return;

            if (data.stage === "Weight") {
                bagEl.classList.add("border-purple-500");
            } 
            else if (data.stage === "Complete") {
                clearanceQueue.push(data);
                if (!isProcessingClearance) {
                    processClearanceQueue();
                }
            }
        });
    } else {
        // Not connected yet — retry in 300ms
        setTimeout(wireSecuritySignalR, 300);
    }
}

function processClearanceQueue() {
    if (clearanceQueue.length === 0) {
        isProcessingClearance = false;
        return;
    }
    isProcessingClearance = true;
    
    const data = clearanceQueue.shift();
    const bagEl = document.getElementById(`bag-${data.id}`);
    
    if (bagEl) {
        if (data.status === "Cleared") {
            bagEl.classList.replace("border-blue-500", "border-green-500");
            bagEl.classList.replace("border-purple-500", "border-green-500");
            bagEl.classList.remove('moving');
            bagEl.classList.add('cleared');
            setTimeout(() => bagEl.remove(), 600); 
        } else {
            bagEl.classList.replace("border-blue-500", "border-red-500");
            bagEl.classList.replace("border-purple-500", "border-red-500");
            bagEl.classList.add("bg-red-50");
            setTimeout(() => {
                bagEl.classList.remove('moving');
                bagEl.classList.add('ejected');
                setTimeout(() => bagEl.remove(), 600);
            }, 300);
        }
        
        if (data.logs) {
            addLogs(data.tag, data.logs);
        }
    }
    
    // Process the next bag in the queue after a realistic delay (800ms)
    // so they visually exit the scanner one-by-one!
    setTimeout(processClearanceQueue, 800);
}

wireSecuritySignalR();

async function manualInspect(id, clearBaggage) {
    try {
        const response = await fetch(`/Security/ManualInspect`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: new URLSearchParams({ baggageId: id, clearBaggage: clearBaggage })
        });
        const result = await response.json();
        
        if (result.success) {
            if (clearBaggage) {
                toastr.success("Baggage manually cleared.");
                if (result.logs) addLogs("MANUAL", result.logs);
            } else {
                toastr.error("Baggage confiscated.");
            }
            fetchPendingBaggages();
        }
    } catch(e) {
        console.error(e);
    }
}

function addLogs(tag, logs) {
    const container = document.getElementById("scanner-logs");
    document.getElementById("logs-empty")?.remove();

    const header = document.createElement("div");
    header.className = "text-blue-400 font-bold mt-2";
    header.innerText = `--- Scanning ${tag} ---`;
    container.prepend(header);

    // Reverse logs so newest are at top
    [...logs].reverse().forEach(log => {
        const li = document.createElement("div");
        li.className = "mb-1";
        if (log.includes("❌") || log.includes("🚨")) {
            li.innerHTML = `<span class="text-red-400">${log}</span>`;
        } else if (log.includes("✅") || log.includes("👨‍✈️")) {
            li.innerHTML = `<span class="text-green-400">${log}</span>`;
        } else {
            li.innerText = log;
        }
        container.prepend(li);
    });
}
