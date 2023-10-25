;; memory: js.mem 1
;; invoke: writeHi
;; expect_call: console.logmem

;; source: https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format
(module
 (import "console" "logmem" (func $log (param i32 i32)))
 (import "js" "mem" (memory 1))
 (data $hi "Hi")        ;; passive data segments are indicated by not having an offset
 (data $there " there") 
 (func (export "writeHi")
    i32.const 0         ;; d = 0 (destination offset)
    i32.const 0         ;; s = 0 (source offset)
    i32.const 2         ;; n = 2 (number of bytes to copy)    
    memory.init $hi     ;; initialize memory at offset d with data at offset s for n bytes
    data.drop $hi       ;; drop the data value
    
    i32.const 2         ;; d = 2 (destination offset)
    i32.const 0         ;; s = 0 (source offset)
    i32.const 6         ;; n = 6 (number of bytes to copy)
    memory.init $there  ;; copy memory at offset d from memory at offset s for n bytes
    data.drop $there    ;; drop the data value
    
    i32.const 0  ;; pass offset 0 to log
    i32.const 8  ;; pass length 8 to log
    call $log))