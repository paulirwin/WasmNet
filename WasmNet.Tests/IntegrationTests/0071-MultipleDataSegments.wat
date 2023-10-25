;; memory: js.mem 1
;; invoke: writeHi
;; expect_call: console.logmem

;; source: https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format
(module
 (import "console" "logmem" (func $log (param i32 i32)))
 (import "js" "mem" (memory 1))
 (data (i32.const 0) "Hi")
 (data (i32.const 2) " there")
 (func (export "writeHi")
   i32.const 0  ;; pass offset 0 to log
   i32.const 8  ;; pass length 8 to log
   call $log))