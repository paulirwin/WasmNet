;; invoke: return
;; expect: (i32:1)

(module
    (func (export "return") (result i32)
        i32.const 1
        return
        i32.const 2
    )
)