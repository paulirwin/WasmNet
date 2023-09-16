;; invoke: xor
;; expect: (i64:962)

(module
    (func (export "xor") (result i64)
        i64.const 42
        i64.const 1000
        i64.xor
    )
)