;; invoke: getAnswerPlus1
;; expect: (i32:43)

;; source: https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format
(module
    (func $getAnswer (result i32)
        i32.const 42)
    (func (export "getAnswerPlus1") (result i32)
        call $getAnswer
        i32.const 1
        i32.add))

