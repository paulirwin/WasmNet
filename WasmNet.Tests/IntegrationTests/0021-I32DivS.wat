;; invoke: div
;; expect: (i32:1)

(module
    (func (export "div") (result i32)
        i32.const 6
        i32.const 4
        i32.div_s
    )
)