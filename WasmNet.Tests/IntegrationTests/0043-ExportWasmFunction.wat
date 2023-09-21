;; invoke: add (i32:3) (i32:4)
;; expect: (i32:7)

;; source: https://developer.mozilla.org/en-US/docs/WebAssembly/Understanding_the_text_format
(module
    (func $add (param $lhs i32) (param $rhs i32) (result i32)
        local.get $lhs
        local.get $rhs
        i32.add)
    (export "add" (func $add))
)
