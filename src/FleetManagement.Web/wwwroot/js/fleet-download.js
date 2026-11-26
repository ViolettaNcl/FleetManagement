window.fleetDownload = (base64, filename, contentType) => {
  const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
  const url = URL.createObjectURL(new Blob([bytes], {type:contentType === "application/json" ? contentType : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"}));
  const link = document.createElement("a");
  link.href = url; link.download = filename;
  document.body.appendChild(link); link.click(); link.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
};
